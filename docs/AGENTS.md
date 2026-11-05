# Documentation index

## SUMMARY

Load only the documents relevant to the work. These files describe the portal and its request-driven Sonos boundary.

| Document | Read for |
|---|---|
| [Architecture](architecture.md) | Resolved profile, projects, worker boundaries and decisions |
| [Getting started](getting-started.md) | Dependencies, tools, local configuration and commands |
| [Integrations](integrations.md) | Pinned OpenPhonos source build and offline testing |
| [Environment](environment.md) | Local deployment, configuration precedence and optional cloud services |
| [Backend](backend/api.md) | Health route, Razor POST and composition |
| [Frontend](frontend/pages.md) | Responsive portal, themes and shared localization |
| [Manual testing](manual-testing.md) | UI verification without live Sonos |
| [Deployment checklist](deployment-checklist.md) | Home deployment tracking |

Use the write-documentation skill for maintaining this tree. Plans under specs/ are maintained by plan-change and are outside this index's content ownership.


## How to work

- The document title is a `#` heading.
- This is followed by the **SUMMARY** section that summarizes what the document contains.
  - The summary is a `##` section
- Check whether a document already covers what you want to document.
- Document each thing once, in the document it pertains to most.
  - Other places should link to that document instead of repeating the information.
  - Use relative links.
  - Use actual Markdown links.
  - Repeating a tagline or summary is acceptable if it links to the relevant document.
- Code is usually the primary source of truth. When documenting, verify facts against the code.
- Update documentation when a change makes existing guidance inaccurate or changes responsibilities, structure, interactions, or documented usage. An implementation change alone does not require a documentation update.
- When a document you update grows beyond the size in [Depth grows with the content](#depth-grows-with-the-content), split it.
- Use Mermaid diagrams where applicable:
  - To describe processes and how they progress from step to step.
  - To describe architecture and the relationships between components and/or systems.
- Documents are meant for both agents and humans.
- Keep things simple and short.
- When documenting `.ps1` or `.sh` scripts, explain their side-effect-free preview switch (`-WhatIf` for PowerShell, `--whatif` for shell) and use it in commands intended for agents. Agents must never run scripts without that flag, including through wrappers or tests. If preview support is missing, inspect the code statically rather than executing it. Scripts are not tested by default and need no test suites, test runners, or unit tests unless the user explicitly requests script tests; do not add routine script-testing requirements to documentation.
- Update the `/docs/AGENTS.md` file as well, if the changes need to be recorded in this table of contents and entry point file.
