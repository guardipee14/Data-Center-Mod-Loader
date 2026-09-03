# Package Compatibility Diagnostics

DCML v0.0.4 evaluates package runtime requirements **after manifest discovery
and before dependency resolution or module activation**.

This lets the loader explain why a package cannot run without executing the
package first.

## DCML version requirements

The existing manifest field is now actively enforced:

```json
{
  "minimumDCMLVersion": "0.0.3"
}
```

If the active runtime is older, DCML reports:

```text
DCML_COMPATIBILITY_DCML_VERSION_UNSATISFIED
```

and excludes that package from activation.

## Required runtime capabilities

Manifest schema version 1 is extended additively with an optional
`requiredCapabilities` array:

```json
{
  "requiredCapabilities": [
    {
      "id": "dcml.events",
      "minimumVersion": "1.0.0"
    }
  ]
}
```

Existing manifests do not need to add this field. When omitted it defaults to
an empty collection.

A requirement may omit `minimumVersion` when only capability presence matters.

### Diagnostic codes

Missing capability:

```text
DCML_COMPATIBILITY_CAPABILITY_MISSING
```

Advertised capability version is too old:

```text
DCML_COMPATIBILITY_CAPABILITY_VERSION_UNSATISFIED
```

Required dependency was itself rejected for compatibility:

```text
DCML_COMPATIBILITY_DEPENDENCY_INCOMPATIBLE
```

## Required vs optional module dependencies

Compatibility rejection propagates through **required** module dependencies.

Example:

```text
A requires unsupported capability -> A incompatible
B requires A                    -> B incompatible
```

It does not propagate through optional dependencies:

```text
A requires unsupported capability -> A incompatible
B optionally uses A                -> B remains eligible
```

This preserves the optional-dependency behavior proven by the real package
integration tests.

## Evaluation order

The MelonLoader host now uses:

```text
manifest discovery
        |
package compatibility evaluation
        |
dependency resolution of compatible packages
        |
runtime activation
```

This ensures an incompatible package cannot reach its constructor,
`Initialize`, or `Start`.

## Live rejection probe

The repository includes:

```text
dcml.probe.compatibility-unsupported
```

Its manifest deliberately requires:

```text
dcml.probe.unsupported-capability >= 1.0.0
```

which the current host does not advertise.

If the package reaches `Initialize`, it writes:

```text
ACTIVATED-UNEXPECTEDLY.txt
```

and throws.

A successful live compatibility proof therefore requires:

- the package is discovered as valid;
- `DCML_COMPATIBILITY_CAPABILITY_MISSING` is logged;
- the package is counted as incompatible;
- the activation marker does not exist;
- existing compatible modules continue running normally.

## Loader acceptance

`requiredCapabilities` is opt-in manifest metadata.

A mod that does not use DCML's optional SDK or Data Center APIs does not need
to declare those capabilities. DCML does not impose capability requirements
merely because a package is loaded by DCML.
