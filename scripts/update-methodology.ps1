param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,

    [switch]$Apply,
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

function Resolve-ExistingPath {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Path does not exist: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Ensure-ChildPath {
    param(
        [string]$Parent,
        [string]$Child
    )

    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
    $childFull = [System.IO.Path]::GetFullPath($Child).TrimEnd('\', '/')

    if ($childFull -eq $parentFull) {
        throw "Refusing to overwrite the source template itself: $childFull"
    }

    return $childFull
}

function Get-MarkdownSectionFirstValue {
    param(
        [string]$Content,
        [string[]]$Headings
    )

    foreach ($heading in $Headings) {
        $escaped = [Regex]::Escape($heading)
        $match = [Regex]::Match($Content, "(?ms)^##\s+$escaped\s*`r?`n(?<body>.*?)(?=^##\s+|\z)")
        if (-not $match.Success) {
            continue
        }

        $lines = $match.Groups["body"].Value -split "`r?`n"
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            if ($trimmed.Length -eq 0) {
                continue
            }
            if ($trimmed.StartsWith("[") -and $trimmed.EndsWith("]")) {
                continue
            }
            return $trimmed
        }
    }

    return $null
}

function Convert-DocumentLanguage {
    param([AllowNull()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "ru"
    }

    $normalized = $Value.Trim().ToLowerInvariant()
    if ($normalized -in @("en", "english", "английский")) {
        return "en"
    }

    return "ru"
}

function Ensure-ProjectSettings {
    param([string]$ProjectRoot)

    $projectPath = Join-Path $ProjectRoot ".specify\project.yml"
    $constitutionPath = Join-Path $ProjectRoot ".specify\memory\constitution.md"

    $language = "ru"
    $confluenceRoot = $null

    if (-not (Test-Path -LiteralPath $constitutionPath)) {
        Write-Host "  no preserved constitution found for project settings migration"
    } else {
        $content = [System.IO.File]::ReadAllText($constitutionPath, [System.Text.Encoding]::UTF8)
        $legacyLanguage = Get-MarkdownSectionFirstValue -Content $content -Headings @("Язык документов", "Document Language")
        $language = Convert-DocumentLanguage -Value $legacyLanguage
        $legacyConfluenceRoot = Get-MarkdownSectionFirstValue -Content $content -Headings @("Корневая страница Confluence", "Confluence root page")
        if (-not [string]::IsNullOrWhiteSpace($legacyConfluenceRoot) -and
            $legacyConfluenceRoot -notmatch '^\[' -and
            $legacyConfluenceRoot -notin @("Не используется", "Not used")) {
            $confluenceRoot = $legacyConfluenceRoot
        }
    }

    if (Test-Path -LiteralPath $projectPath) {
        $existing = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
        if ($existing -match '(?m)^\s*document_language\s*:') {
            Write-Host "  keep existing .specify\project.yml document_language"
        } else {
            $existing = $existing.TrimEnd() + [Environment]::NewLine + @"

project:
  document_language: "$language"
"@
        }

        if ($existing -notmatch '(?m)^\s*initiative\s*:') {
            $existing = $existing.TrimEnd() + [Environment]::NewLine + @"

initiative:
  class: null
  class_name: null
  classified_at: null
  decision_owner: null
  confidence: null
  rationale: null
  workflow:
    base: "speckit"
    profile: null
    overlay: null
"@
        }

        if ($existing -match '(?m)^\s*spec_root_page_url\s*:') {
            Write-Host "  keep existing .specify\project.yml spec_root_page_url"
        } elseif ($existing -match '(?m)^  confluence:\s*$') {
            $rootValue = if ($null -eq $confluenceRoot) { "null" } else { '"' + $confluenceRoot.Replace('"', '\"') + '"' }
            $existing = $existing -replace '(?m)^  confluence:\s*$', ("  confluence:" + [Environment]::NewLine + "    spec_root_page_url: $rootValue")
        }

        [System.IO.File]::WriteAllText($projectPath, $existing.TrimEnd() + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
        Write-Host "  migrated .specify\project.yml settings from legacy constitution where needed"
        return
    }

    $rootYaml = if ($null -eq $confluenceRoot) { "null" } else { '"' + $confluenceRoot.Replace('"', '\"') + '"' }
    $projectYaml = @"
schema_version: "1.1"

project:
  document_language: "$language"

initiative:
  class: null
  class_name: null
  classified_at: null
  decision_owner: null
  confidence: null
  rationale: null
  workflow:
    base: "speckit"
    profile: null
    overlay: null

integrations:
  jira:
    sync_enabled: true
  confluence:
    sync_enabled: false
    spec_root_page_url: $rootYaml
"@

    $projectParent = Split-Path -Parent $projectPath
    New-Item -ItemType Directory -Path $projectParent -Force | Out-Null
    [System.IO.File]::WriteAllText($projectPath, $projectYaml.TrimEnd() + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    Write-Host "  created .specify\project.yml with migrated project settings"
}

function Update-CodexManifest {
    param(
        [string]$ProjectRoot,
        [string]$SourceRoot
    )

    $manifestPath = Join-Path $ProjectRoot ".specify\integrations\codex.manifest.json"
    $sourceManifestPath = Join-Path $SourceRoot ".specify\integrations\codex.manifest.json"
    $skillsRoot = Join-Path $ProjectRoot ".agents\skills"

    if (-not (Test-Path -LiteralPath $skillsRoot)) {
        Write-Host "  skip .specify\integrations\codex.manifest.json refresh: no .agents\skills directory"
        return
    }

    $version = "1.0.1"
    if (Test-Path -LiteralPath $sourceManifestPath) {
        try {
            $sourceManifest = Get-Content -Encoding UTF8 -LiteralPath $sourceManifestPath -Raw | ConvertFrom-Json
            if (-not [string]::IsNullOrWhiteSpace($sourceManifest.version)) {
                $version = $sourceManifest.version
            }
        } catch {
            Write-Host "  warning: could not read source codex.manifest.json version; using $version"
        }
    }

    $projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/')
    $files = [ordered]@{}
    Get-ChildItem -LiteralPath $skillsRoot -Recurse -File |
        Where-Object { $_.Name -eq "SKILL.md" -or $_.FullName -match '[\\/]agents[\\/]openai\.yaml$' } |
        Sort-Object FullName |
        ForEach-Object {
            $fullPath = [System.IO.Path]::GetFullPath($_.FullName)
            $relativePath = $fullPath.Substring($projectRootFull.Length).TrimStart('\', '/').Replace('\', '/')
            $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
            $files[$relativePath] = $hash
        }

    $manifest = [ordered]@{
        integration = "codex"
        version = $version
        installed_at = [DateTimeOffset]::UtcNow.ToString("o")
        files = $files
    }

    $manifestParent = Split-Path -Parent $manifestPath
    New-Item -ItemType Directory -Path $manifestParent -Force | Out-Null
    $json = $manifest | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText($manifestPath, $json + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    Write-Host "  refreshed .specify\integrations\codex.manifest.json from .agents\skills"
}

function Test-CodexSkillMetadata {
    param([string]$ProjectRoot)

    $skillsRoot = Join-Path $ProjectRoot ".agents\skills"
    $explicitSkillNames = @(
        "speckit-analyze", "speckit-archive-run", "speckit-changelog-diff",
        "speckit-changelog-generate", "speckit-changelog-notify", "speckit-changelog-release",
        "speckit-checklist", "speckit-clarify", "speckit-confluence-read",
        "speckit-confluence-update", "speckit-confluence-write", "speckit-constitution",
        "speckit-converge", "speckit-critique-run", "speckit-implement", "speckit-init",
        "speckit-plan", "speckit-specify", "speckit-tasks", "speckit-taskstoissues",
        "speckit-verify-run"
    )
    if (-not (Test-Path -LiteralPath $skillsRoot)) {
        throw "Codex skills directory is missing: $skillsRoot"
    }

    $errors = @()
    Get-ChildItem -LiteralPath $skillsRoot -Directory | Sort-Object Name | ForEach-Object {
        $isManagedSkill = $_.Name -like "speckit-*"
        $skillPath = Join-Path $_.FullName "SKILL.md"
        if (-not (Test-Path -LiteralPath $skillPath)) {
            return
        }

        $skillBytes = [System.IO.File]::ReadAllBytes($skillPath)
        if ($skillBytes.Length -ge 3 -and
            $skillBytes[0] -eq 0xEF -and
            $skillBytes[1] -eq 0xBB -and
            $skillBytes[2] -eq 0xBF) {
            $errors += "$($_.Name): SKILL.md must be UTF-8 without BOM"
        }

        $skillContent = [System.IO.File]::ReadAllText($skillPath, [System.Text.Encoding]::UTF8)
        if (-not $skillContent.StartsWith("---" + [Environment]::NewLine) -and
            -not $skillContent.StartsWith("---`n")) {
            $errors += "$($_.Name): SKILL.md must start with YAML frontmatter delimiter ---"
        }

        $metadataPath = Join-Path $_.FullName "agents\openai.yaml"
        if (-not (Test-Path -LiteralPath $metadataPath)) {
            if ($isManagedSkill) {
                $errors += "$($_.Name): managed skill is missing agents/openai.yaml"
            }
            return
        }

        $metadata = [System.IO.File]::ReadAllText($metadataPath, [System.Text.Encoding]::UTF8)
        $shortDescription = [Regex]::Match($metadata, '(?m)^\s*short_description:\s*"(?<value>[^"]+)"\s*$')
        if (-not $shortDescription.Success) {
            $errors += "$($_.Name): missing quoted interface.short_description"
            return
        }

        $length = $shortDescription.Groups["value"].Value.Length
        if ($length -lt 25 -or $length -gt 64) {
            $errors += "$($_.Name): interface.short_description must contain 25-64 characters"
        }

        if ($isManagedSkill -and $_.Name -in $explicitSkillNames -and
            $metadata -notmatch '(?ms)^policy:\s*.*?^\s*allow_implicit_invocation:\s*false\s*$') {
            $errors += "$($_.Name): user-facing command must set policy.allow_implicit_invocation to false"
        }
    }

    if ($errors.Count -gt 0) {
        throw "Codex skill validation failed:`n  $($errors -join "`n  ")"
    }

    Write-Host "  validated BOM-free SKILL.md files, Codex UI metadata, and invocation policy"
}

function Get-RelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not $pathFull.StartsWith($rootFull + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
        -not $pathFull.StartsWith($rootFull + [System.IO.Path]::AltDirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
        $pathFull -ne $rootFull) {
        throw "Path is outside root: $Path"
    }

    return $pathFull.Substring($rootFull.Length).TrimStart('\', '/').Replace('\', '/')
}

function Read-ManagedManifestFiles {
    param([string]$ProjectRoot)

    $files = [ordered]@{}
    $manifestRoot = Join-Path $ProjectRoot ".specify\integrations"
    if (-not (Test-Path -LiteralPath $manifestRoot)) {
        return $files
    }

    $knownManifests = @(
        "codex.manifest.json",
        "speckit.manifest.json",
        "corporate-methodology.manifest.json"
    )

    foreach ($manifestName in $knownManifests) {
        $manifestPath = Join-Path $manifestRoot $manifestName
        if (-not (Test-Path -LiteralPath $manifestPath)) {
            continue
        }

        try {
            $manifest = Get-Content -Encoding UTF8 -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        } catch {
            Write-Host "  warning: could not read manifest $manifestName; preserving its files"
            continue
        }

        if ($null -eq $manifest.files) {
            continue
        }

        $manifest.files.PSObject.Properties | ForEach-Object {
            $relativePath = $_.Name.Replace('\', '/').TrimStart('/')
            if ($relativePath.Contains("..")) {
                Write-Host "  warning: ignored suspicious manifest path: $relativePath"
                return
            }
            $files[$relativePath] = $true
        }
    }

    return $files
}

function Get-SourceMethodologyFiles {
    param(
        [string]$SourceRoot,
        [string[]]$ManagedItems
    )

    $files = [ordered]@{}
    foreach ($relativePath in $ManagedItems) {
        $sourceItem = Join-Path $SourceRoot $relativePath
        if (-not (Test-Path -LiteralPath $sourceItem)) {
            continue
        }

        $item = Get-Item -LiteralPath $sourceItem
        if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $sourceItem -Recurse -File | ForEach-Object {
                $files[(Get-RelativePath -Root $SourceRoot -Path $_.FullName)] = $true
            }
        } else {
            $files[(Get-RelativePath -Root $SourceRoot -Path $sourceItem)] = $true
        }
    }

    return $files
}

function Remove-EmptyParents {
    param(
        [string]$ProjectRoot,
        [string]$Path,
        [string[]]$StopRelativeRoots
    )

    $projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/')
    $current = Split-Path -Parent $Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $currentFull = [System.IO.Path]::GetFullPath($current).TrimEnd('\', '/')
        if ($currentFull -eq $projectRootFull) {
            break
        }

        $relative = $currentFull.Substring($projectRootFull.Length).TrimStart('\', '/').Replace('\', '/')
        if ($relative -in $StopRelativeRoots) {
            break
        }

        if ((Get-ChildItem -LiteralPath $currentFull -Force | Select-Object -First 1)) {
            break
        }

        Remove-Item -LiteralPath $currentFull -Force
        $current = Split-Path -Parent $currentFull
    }
}

function Update-ManagedItem {
    param(
        [string]$SourceRoot,
        [string]$TargetRoot,
        [string]$RelativePath,
        [System.Collections.IDictionary]$PreviousManagedFiles,
        [System.Collections.IDictionary]$SourceManagedFiles,
        [string[]]$ManagedRoots
    )

    $sourceItem = Join-Path $SourceRoot $RelativePath
    $targetItem = Join-Path $TargetRoot $RelativePath

    if (-not (Test-Path -LiteralPath $sourceItem)) {
        return
    }

    $relativePrefix = $RelativePath.Replace('\', '/').TrimEnd('/') + '/'
    foreach ($managedPath in @($PreviousManagedFiles.Keys)) {
        $normalized = $managedPath.Replace('\', '/')
        $insideItem = $normalized -eq $RelativePath.Replace('\', '/') -or $normalized.StartsWith($relativePrefix, [StringComparison]::OrdinalIgnoreCase)
        if (-not $insideItem -or $SourceManagedFiles.Contains($normalized)) {
            continue
        }

        $targetManagedPath = Join-Path $TargetRoot $normalized.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $targetManagedPath -PathType Leaf) {
            Remove-Item -LiteralPath $targetManagedPath -Force
            Remove-EmptyParents -ProjectRoot $TargetRoot -Path $targetManagedPath -StopRelativeRoots $ManagedRoots
        }
    }

    $item = Get-Item -LiteralPath $sourceItem
    if ($item.PSIsContainer) {
        New-Item -ItemType Directory -Path $targetItem -Force | Out-Null
        Get-ChildItem -LiteralPath $sourceItem -Recurse -File | ForEach-Object {
            $relativeFile = Get-RelativePath -Root $SourceRoot -Path $_.FullName
            $targetFile = Join-Path $TargetRoot $relativeFile.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
            $targetParent = Split-Path -Parent $targetFile
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $targetFile -Force
        }
    } else {
        $targetParent = Split-Path -Parent $targetItem
        New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        Copy-Item -LiteralPath $sourceItem -Destination $targetItem -Force
    }
}

function Update-MethodologyManifest {
    param(
        [string]$ProjectRoot,
        [string]$SourceRoot,
        [string[]]$ManagedItems
    )

    $manifestPath = Join-Path $ProjectRoot ".specify\integrations\corporate-methodology.manifest.json"
    $sourceFiles = Get-SourceMethodologyFiles -SourceRoot $SourceRoot -ManagedItems $ManagedItems
    $files = [ordered]@{}

    foreach ($relativePath in ($sourceFiles.Keys | Sort-Object)) {
        $targetFile = Join-Path $ProjectRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $targetFile -PathType Leaf) {
            $files[$relativePath] = (Get-FileHash -LiteralPath $targetFile -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

    $manifest = [ordered]@{
        integration = "corporate-methodology"
        version = "1.0.1"
        installed_at = [DateTimeOffset]::UtcNow.ToString("o")
        files = $files
    }

    $manifestParent = Split-Path -Parent $manifestPath
    New-Item -ItemType Directory -Path $manifestParent -Force | Out-Null
    $json = $manifest | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText($manifestPath, $json + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    Write-Host "  refreshed .specify\integrations\corporate-methodology.manifest.json from managed methodology files"
}

$scriptPath = $PSCommandPath
if (-not $scriptPath) {
    $scriptPath = $MyInvocation.MyCommand.Path
}

$scriptDir = Split-Path -Parent $scriptPath
$sourceRoot = Resolve-ExistingPath (Join-Path $scriptDir "..")
$targetRoot = Resolve-ExistingPath $TargetPath
$targetRoot = Ensure-ChildPath -Parent $sourceRoot -Child $targetRoot

if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot ".specify"))) {
    throw "Source template does not look like a Spec Kit methodology template: $sourceRoot"
}

if (-not (Test-Path -LiteralPath (Join-Path $targetRoot ".specify"))) {
    throw "Target does not look like an existing Spec Kit project: $targetRoot"
}

$itemsToReplace = @(
    ".agents\skills",
    ".specify\extensions",
    ".specify\integrations",
    ".specify\scripts",
    ".specify\templates",
    ".specify\workflows",
    ".specify\extensions.yml",
    ".specify\integration.json",
    ".specify\.gitignore",
    "AGENTS.md",
    "scripts",
    "UPDATE_FROM_TEMPLATE.md",
    "METHODOLOGY.md",
    "SPEC_KIT_CUSTOMIZATIONS.md"
)

$preservedItems = @(
    "specs",
    ".specify\memory",
    ".specify\project.yml",
    ".specify\jira-constitution-mapping.json",
    ".specify\traces",
    "README.md"
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $targetRoot ".specify\backups\methodology-update-$timestamp"

Write-Host "Corporate methodology update"
Write-Host "Source: $sourceRoot"
Write-Host "Target: $targetRoot"
Write-Host ""

if (-not $Apply) {
    Write-Host "Mode: preview only. Re-run with -Apply to make changes."
} else {
    Write-Host "Mode: apply changes."
}

if ($NoBackup) {
    Write-Host "Backup: disabled."
} else {
    Write-Host "Backup: $backupRoot"
}

Write-Host ""
Write-Host "Will replace methodology files:"
foreach ($relativePath in $itemsToReplace) {
    $sourceItem = Join-Path $sourceRoot $relativePath
    if (Test-Path -LiteralPath $sourceItem) {
        Write-Host "  update $relativePath"
    } else {
        Write-Host "  skip missing in source: $relativePath"
    }
}
Write-Host "  note: managed methodology files are overwritten from the template"
Write-Host "  note: files tracked by previous methodology manifests but missing in the new template are removed"
Write-Host "  note: untracked project-specific files and directories under these paths are preserved"

Write-Host ""
Write-Host "Will preserve project data:"
foreach ($relativePath in $preservedItems) {
    Write-Host "  preserve $relativePath"
}

Write-Host ""
Write-Host "Will migrate preserved project settings when needed:"
Write-Host "  ensure .specify\project.yml has project.document_language"
Write-Host "  ensure .specify\project.yml has initiative class state"
Write-Host "  ensure .specify\project.yml has integrations.confluence.spec_root_page_url"
Write-Host "  validate BOM-free SKILL.md files plus agents\openai.yaml metadata and policy"
Write-Host "  refresh .specify\integrations\codex.manifest.json from .agents\skills"

Test-CodexSkillMetadata -ProjectRoot $sourceRoot

if (-not $Apply) {
    exit 0
}

if (-not $NoBackup) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
}

$previousManagedFiles = Read-ManagedManifestFiles -ProjectRoot $targetRoot
$sourceManagedFiles = Get-SourceMethodologyFiles -SourceRoot $sourceRoot -ManagedItems $itemsToReplace
$managedRoots = @($itemsToReplace | ForEach-Object { $_.Replace('\', '/').TrimEnd('/') })

foreach ($relativePath in $itemsToReplace) {
    $sourceItem = Join-Path $sourceRoot $relativePath
    $targetItem = Join-Path $targetRoot $relativePath

    if (-not (Test-Path -LiteralPath $sourceItem)) {
        continue
    }

    if ((Test-Path -LiteralPath $targetItem) -and (-not $NoBackup)) {
        $backupItem = Join-Path $backupRoot $relativePath
        $backupParent = Split-Path -Parent $backupItem
        New-Item -ItemType Directory -Path $backupParent -Force | Out-Null
        Copy-Item -LiteralPath $targetItem -Destination $backupItem -Recurse -Force
    }

    Update-ManagedItem `
        -SourceRoot $sourceRoot `
        -TargetRoot $targetRoot `
        -RelativePath $relativePath `
        -PreviousManagedFiles $previousManagedFiles `
        -SourceManagedFiles $sourceManagedFiles `
        -ManagedRoots $managedRoots
}

Ensure-ProjectSettings -ProjectRoot $targetRoot
Test-CodexSkillMetadata -ProjectRoot $targetRoot
Update-CodexManifest -ProjectRoot $targetRoot -SourceRoot $sourceRoot
Update-MethodologyManifest -ProjectRoot $targetRoot -SourceRoot $sourceRoot -ManagedItems $itemsToReplace

Write-Host ""
Write-Host "Update complete."
Write-Host "Check the methodology version in METHODOLOGY.md."

if (-not $NoBackup) {
    Write-Host "Backup created at: $backupRoot"
}
