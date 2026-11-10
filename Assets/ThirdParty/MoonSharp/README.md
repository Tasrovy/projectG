# MoonSharp

Installed 2026-11-03 for Lua story-condition scripts.

- Version: NuGet MoonSharp 2.0.0 (upstream stable 2.0.0.0).
- Source: https://api.nuget.org/v3-flatcontainer/moonsharp/2.0.0/moonsharp.2.0.0.nupkg
- Assembly: lib/net40-client/MoonSharp.Interpreter.dll, with API XML documentation.
- DLL SHA256: 1DB76110F21698639F55D28E21BDDB536C0C497CEB741DEE49FEDCCA9BCD1588
- Upstream: https://github.com/moonsharp-devs/moonsharp
- License: LICENSE.txt.

Local smoke test: created Script(CoreModules.Preset_HardSandbox), evaluated `local stats = { nature1 = 35 }; return stats.nature1 >= 30`, received Boolean true.

Unity compilation and target-platform builds remain to be verified. Dependency installation does not implement story selection or condition evaluation integration. Future integration should require Boolean returns, isolate condition state, restrict available APIs, and enforce an execution budget.
