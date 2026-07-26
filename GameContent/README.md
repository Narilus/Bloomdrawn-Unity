# Bloomdrawn canonical content

- `production/` is reserved for validated hand-authored production YAML. It remains empty during M0.
- `fixtures/` contains isolated, non-production YAML used only by import/registry tests.
- `generated/` is a reproducible JSON output location. Generated files are derivatives, never an authored source of truth.

The runtime does not parse YAML. Editor/build tooling validates canonical YAML before any registry is created.
