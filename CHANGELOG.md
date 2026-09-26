# Changelog

## 1.3.0

Added the following methods for `SHAssemblyFunction`, `SHAssemblyFunction<TFunction>` and `SHFunction2<TFunction>`:
- `SetResult`: Sets the target address for the function wrapper/hook from code.
- `IsHookEnabled`: Check if the function hook is enabled if there is one, otherwise it always returns false.
- `ToggleEnabled`: Toggles the function hook between being enabled and disabled
- `Disable`: Disables a function hook. The C#/Assembly code will not be called when the game calls the function.
- `Enable`: Re-enables a function hook. The C#/Assembly code will be called again when the game calls the function.

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