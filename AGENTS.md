# Project rules

## Artifact policy

Creating and updating project artifacts is authorized.

## Draft preservation

The authoritative source drafts for GitHub Spec Kit are in
`preparation/docs/product-specs/`. Do not rename, move, edit, delete, or
overwrite any file in this directory. Use these files only as source material
when creating or updating Spec Kit artifacts.

Keep Spec Kit outputs separate from the source drafts:

- Constitution: `.specify/memory/constitution.md`
- Feature specification and its follow-on artifacts: `specs/<feature>/`

When invoking a Spec Kit skill, explicitly name the relevant source-draft
path and state that it is immutable. Never set `SPECIFY_FEATURE_DIRECTORY` to
`preparation/docs/product-specs/`.
