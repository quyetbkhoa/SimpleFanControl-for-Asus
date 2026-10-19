# Runtime packaging dependency

Place Microsoft Sysinternals `PsExec.exe` in this directory before running:

```powershell
.\scripts\package.ps1
```

The binary is intentionally ignored by Git. It is copied into the local release
package because compatible Asus hardware may require the GUI to run as SYSTEM.
