/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class WindowsCredentialManager
{
    // CRED_PERSIST_SESSION — credential is removed at logoff.
    // We always use Session: the entry is also deleted manually a few seconds after the launch,
    // so wider persistence scopes (LocalMachine, Enterprise) bring no benefit and a larger blast radius.
    private const uint CRED_PERSIST_SESSION = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public IntPtr LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree(IntPtr credentialPtr);

    private static bool Exists(string target, WindowsCredentialType type)
    {
        if (CredRead(target, (uint)type, 0, out var credPtr) && credPtr != IntPtr.Zero)
        {
            CredFree(credPtr);
            return true;
        }
        return false;
    }

    private static bool Add(string target, WindowsCredentialType type, string userName, string password)
    {
        var cred = new CREDENTIAL
        {
            Type = (uint)type,
            TargetName = target,
            UserName = userName,
            Persist = CRED_PERSIST_SESSION
        };

        byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
        cred.CredentialBlobSize = (uint)passwordBytes.Length;
        cred.CredentialBlob = Marshal.AllocCoTaskMem(passwordBytes.Length);
        Marshal.Copy(passwordBytes, 0, cred.CredentialBlob, passwordBytes.Length);

        try
        {
            return CredWrite(ref cred, 0);
        }
        finally
        {
            Marshal.FreeCoTaskMem(cred.CredentialBlob);
        }
    }

    private static bool Delete(string target, WindowsCredentialType type) => CredDelete(target, (uint)type, 0);

    /// <summary>
    /// If credentials are provided, injects them temporarily into the Windows Vault using the
    /// given <paramref name="target"/> and <paramref name="type"/>, runs the action, then removes
    /// the entry after a short delay.
    /// If credentials are null or empty, just runs the action.
    /// If the credential already existed, it is not touched.
    /// </summary>
    public static void WithTemporaryCredential(string target,
                                               WindowsCredentialType type,
                                               Credentials? credentials,
                                               Action action)
    {
        var hasCredentials = credentials is { Username.Length: > 0, Password.Length: > 0 };
        if (!hasCredentials)
        {
            action();
            return;
        }

        var alreadyExisted = Exists(target, type);
        if (!alreadyExisted) { Add(target, type, credentials!.Username, credentials.Password); }

        action();

        if (!alreadyExisted)
        {
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                Delete(target, type);
            });
        }
    }
}
