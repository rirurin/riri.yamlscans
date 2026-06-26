# Changelog

## 1.1.0

*Version numbers are now synchronised between `riri.yamlscans` and `riri.yamlscans.ReloadedII`*

Added SHStatic wrapper for handling pointers to static data within the executable.

## 1.0.2

For `riri.yamlscans v1.0.2` and `riri.yamlscans.ReloadedII v1.0.0` 

- Added riri.yamlscans.ReloadedII library as an adapter to interface with Reloaded-II mods without requiring the mod developer to implement it themselves.
- Add `TryDeref` to the custom expression language for signature transforms
- Update NCalcSync to 6.3.1 to [avoid vulnerability](https://github.com/advisories/GHSA-3w5p-95mh-gq75)

## 1.0.1

Added `DerefData` method for `CustomExpression` to dereference a pointer to a VTable entry or other static data structures that store a full-size absolute pointer.

## 1.0.0

Initial release.