using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babe.Lua
{
/**//// <summary>
    /// 该类要使用在windows 5.1 以后的版本，如果你的windows很旧，就把Windbg里面的dll拷贝过来，一般都没有问题
    /// DbgHelp.dll 是windows自带的 dll文件 。
    /// </summary>
    public static class MiniDump
    {
        /**//*
         * 导入DbgHelp.dll
         */
        [DllImport("DbgHelp.dll")]
        private static extern Boolean MiniDumpWriteDump(
                                    IntPtr hProcess,
                                    Int32 processId,
                                    IntPtr fileHandle,
                                    MiniDumpType dumpType, 
                                    ref MinidumpExceptionInfo excepInfo,
                                    IntPtr userInfo, 
                                    IntPtr extInfo );

        /**//*
         *  MINIDUMP_EXCEPTION_INFORMATION  这个宏的信息
         */
        struct MinidumpExceptionInfo
        {
            public Int32 ThreadId;
            public IntPtr ExceptionPointers;
            public Boolean ClientPointers;
        }

        /**//*
         * 自己包装的一个函数
         */
        public static Boolean TryDump(String dmpPath, MiniDumpType dmpType)
        {
            try
            {
                //使用文件流来创健 .dmp文件
                using (FileStream stream = new FileStream(dmpPath, FileMode.Create))
                {
                    //取得进程信息
                    Process process = Process.GetCurrentProcess();

                    // MINIDUMP_EXCEPTION_INFORMATION 信息的初始化
                    MinidumpExceptionInfo mei = new MinidumpExceptionInfo();

                    mei.ThreadId = Thread.CurrentThread.ManagedThreadId;
                    mei.ExceptionPointers = Marshal.GetExceptionPointers();
                    mei.ClientPointers = true;


                    //这里调用的Win32 API
                    Boolean res = MiniDumpWriteDump(
                                        process.Handle,
                                        process.Id,
                                        stream.SafeFileHandle.DangerousGetHandle(),
                                        dmpType,
                                        ref mei,
                                        IntPtr.Zero,
                                        IntPtr.Zero);

                    //清空 stream
                    stream.Flush();
                    stream.Close();

                    return res;
                }
            }
            catch
            {
                return false;
            }
        }

        public enum MiniDumpType
        {
            None = 0x00010000,
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            FilterMemory = 0x00000008,
            ScanMemory = 0x00000010,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            FilterModulePaths = 0x00000080,
            WithProcessThreadData = 0x00000100,
            WithPrivateReadWriteMemory = 0x00000200,
            WithoutOptionalData = 0x00000400,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00001000,
            WithCodeSegs = 0x00002000
        }
    }

}
