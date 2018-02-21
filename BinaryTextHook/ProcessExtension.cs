using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTextHook {
    public static class ProcessExtension {
        [Flags]
        public enum ThreadAccess : int {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        public static void Suspend(int pid) {
            //Console.WriteLine("suspending: " + pid);
            Process process = Process.GetProcessById(pid);

            Action<ProcessThread> suspend = pt => {
                var threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);

                if (threadHandle != IntPtr.Zero) {
                    try {
                        SuspendThread(threadHandle);
                    } finally {
                        CloseHandle(threadHandle);
                    }
                };
            };

            var threads = process.Threads.Cast<ProcessThread>().ToArray();

            if (threads.Length > 1) {
                Parallel.ForEach(threads, new ParallelOptions { MaxDegreeOfParallelism = threads.Length }, pt => {
                    suspend(pt);
                });
            } else {
                suspend(threads[0]);
            }
        }

        public static void Resume(int pid) {
            //Console.WriteLine("resuming: " + pid);
            Process process = Process.GetProcessById(pid);

            Action<ProcessThread> resume = pt => {
                var threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);

                if (threadHandle != IntPtr.Zero) {
                    try {
                        ResumeThread(threadHandle);
                    } finally {
                        CloseHandle(threadHandle);
                    }
                }
            };

            var threads = process.Threads.Cast<ProcessThread>().ToArray();

            if (threads.Length > 1) {
                Parallel.ForEach(threads, new ParallelOptions { MaxDegreeOfParallelism = threads.Length }, pt => {
                    resume(pt);
                });
            } else {
                resume(threads[0]);
            }
        }
    }
}
