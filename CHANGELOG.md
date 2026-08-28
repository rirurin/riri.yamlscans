# Changelog

## 1.2.2

- Fixed error when reading an empty YAML by returning a ScanModel with no entries instead

## 1.2.1

- Add `DerefInt` and `GetAddressFromInt` transformer methods for handling instructions that store int-sized pointers relative to the program's base address.
- Added an optional `onScanFound` callback for `SHFunction2<TFunction>` and `SHStatic<TStatic>`

## 1.2.0

Added `SHAssembly` and `SHAssembly<TFunction>` for creating mid-function assembly hooks.

## 1.1.2
Removed or set some debug logging to Verbose mode

## 1.1.1

`riri.yamlscans.ReloadedII`:
- Fix crash when trying to check signatures in an empty folder

## 1.1.0

*Version numbers are now synchronised between `riri.yamlscans` and `riri.yamlscans.ReloadedII`*

Added SHStatic wrapper for handling pointers to static data within the executable.