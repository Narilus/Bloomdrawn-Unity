# Acceptance Manifests

The Bloom Planner may define task-specific acceptance manifests here. The Builder is denied edits to the entire `acceptance/` tree.

These manifests are contracts, not yet an immutable external runner. A later workflow task should place executable black-box acceptance outside the Builder's writable workspace or otherwise enforce hashes/permissions mechanically.
