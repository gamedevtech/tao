#region License
/*
MIT License
Copyright �2003-2006 Tao Framework Team
http://www.taoframework.com
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Tao.GlBindGen
{
    static partial class SpecWriter
    {
        #region WriteSpecs
        public static void WriteSpecs(string output_path, List<Function> functions, List<Function> wrappers, List<Constant> constants)
        {
            string filename = Path.Combine(output_path, Properties.Bind.Default.OutputClass + ".cs");

            if (!Directory.Exists(Properties.Bind.Default.OutputPath))
                Directory.CreateDirectory(Properties.Bind.Default.OutputPath);

            StreamWriter sw = new StreamWriter(filename, false);

            Console.WriteLine("Writing Tao.OpenGl.Gl class to {0}", filename);

            WriteLicense(sw);

            sw.WriteLine("using System;");
            sw.WriteLine("using System.Runtime.InteropServices;");
            sw.WriteLine();
            sw.WriteLine("namespace {0}", Properties.Bind.Default.OutputNamespace);
            sw.WriteLine("{");

            WriteTypes(sw);

            sw.WriteLine("    public static partial class {0}", Properties.Bind.Default.OutputClass);
            sw.WriteLine("    {");

            WriteConstants(sw, constants);
            WriteFunctionSignatures(sw, functions);
            WriteDllImports(sw, functions);
            WriteFunctions(sw, functions);
            WriteWrappers(sw, wrappers);
            WriteConstructor(sw, functions);
            WriteGetAddress(sw);

            sw.WriteLine("    }");
            sw.WriteLine("}");
            sw.WriteLine();

            sw.Flush();
            sw.Close();
        }
        #endregion

        private static void WriteLicense(StreamWriter sw)
        {
        }

        #region WriteTypes
        private static void WriteTypes(StreamWriter sw)
        {
            sw.WriteLine("    #region Types");
            //foreach ( c in constants)
            foreach (string key in Translation.CStypes.Keys)
            {
                sw.WriteLine("    using {0} = System.{1};", key, Translation.CStypes[key]);
                //sw.WriteLine("        public const {0};", c.ToString());
            }
            sw.WriteLine("    #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write constants
        private static void WriteConstants(StreamWriter sw, List<Constant> constants)
        {
            sw.WriteLine("        #region Constants");

            foreach (Constant c in constants)
            {
                sw.WriteLine("        public const GLuint {0};", c.ToString());
            }

            sw.WriteLine("        #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write function signatures
        private static void WriteFunctionSignatures(StreamWriter sw, List<Function> functions)
        {
            sw.WriteLine("        #region Function signatures");
            sw.WriteLine();
            sw.WriteLine("        public static class Delegates");
            sw.WriteLine("        {");

            foreach (Function f in functions)
            {
                sw.WriteLine("            public delegate {0};", f.ToString());
            }

            sw.WriteLine("        }");
            sw.WriteLine("        #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write dll imports
        private static void WriteDllImports(StreamWriter sw, List<Function> functions)
        {
            sw.WriteLine("        #region Imports");
            sw.WriteLine();
            sw.WriteLine("        internal class Imports");
            sw.WriteLine("        {");

            foreach (Function f in functions)
            {
                if (!f.Extension)
                {
                    sw.WriteLine("            [DllImport(\"opengl32\", EntryPoint = \"{0}\")]", f.Name.TrimEnd('_'));
                    sw.WriteLine("            public static extern {0};", f.ToString());
                }
            }

            sw.WriteLine("        }");
            sw.WriteLine("        #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write functions
        private static void WriteFunctions(StreamWriter sw, List<Function> functions)
        {
            sw.WriteLine("        #region Function initialisation");
            sw.WriteLine();

            foreach (Function f in functions)
            {
                sw.WriteLine("        public static Delegates.{0} {0} = (Delegates.{0})GetAddress(\"{1}\", typeof(Delegates.{0}));", f.Name, f.Name.TrimEnd('_'));
            }

            sw.WriteLine("        #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write constructor
        private static void WriteConstructor(StreamWriter sw, List<Function> functions)
        {
            sw.WriteLine("        #region static Constructor");
            sw.WriteLine();
            sw.WriteLine("        static {0}()", Properties.Bind.Default.OutputClass);
            sw.WriteLine("        {");

            #region Older Windows Core

            {
                // Load core for older windows versions.
                sw.WriteLine("            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major < 6 || Environment.OSVersion.Platform == PlatformID.Win32Windows)");
                sw.WriteLine("            {");
                sw.WriteLine("                #region Older Windows Core");

                string[] import_list = { "1.0", "1.1" };
                foreach (Function f in functions)
                {
                    if (IsImportFunction(f, import_list))
                        sw.WriteLine("                {0}.{1} = new {0}.Delegates.{1}(Imports.{1});",
                            Properties.Bind.Default.OutputClass,
                            f.Name);
                    /*else
                        sw.WriteLine("                {0}.{1} = ({0}.Delegates.{1}) GetAddress(\"{1}\", typeof({0}.Delegates.{1}));",
                            Properties.Bind.Default.OutputTaoClass,
                            f.Name);*/
                }
                sw.WriteLine("                #endregion");
                sw.WriteLine("            }");
            }

            #endregion

            #region Windows Vista Core

            {
                // Load core for windows vista.
                sw.WriteLine("            else if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)");
                sw.WriteLine("            {");
                sw.WriteLine("                #region Windows Vista Core");
                //sw.WriteLine("                GetAddress = new GetAddressPrototype(WindowsGetAddress);");
                string[] import_list = { "1.0", "1.1", "1.2", "1.3", "1.4" };
                foreach (Function f in functions)
                {
                    if (IsImportFunction(f, import_list))
                        sw.WriteLine("                {0}.{1} = new {0}.Delegates.{1}(Imports.{1});",
                            Properties.Bind.Default.OutputClass,
                            f.Name);
                    /*else
                        sw.WriteLine("                {0}.{1} = ({0}.Delegates.{1}) GetAddress(\"{1}\", typeof({0}.Delegates.{1}));",
                            Properties.Bind.Default.OutputTaoClass,
                            f.Name);*/
                }
                sw.WriteLine("                #endregion");
                sw.WriteLine("            }");
            }

            #endregion

            #region X11 Core

            {
                // Load core for windows X11.
                sw.WriteLine("            else if (Environment.OSVersion.Platform == PlatformID.Unix)");
                sw.WriteLine("            {");
                sw.WriteLine("                #region X11 Core");
                //sw.WriteLine("                GetAddress = new GetAddressPrototype(X11GetAddress);");
                string[] import_list = { "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "2.0" };
                foreach (Function f in functions)
                {
                    if (IsImportFunction(f, import_list))
                        sw.WriteLine("                {0}.{1} = new {0}.Delegates.{1}(Imports.{1});",
                            Properties.Bind.Default.OutputClass,
                            f.Name);
                    /*else
                        sw.WriteLine("                {0}.{1} = ({0}.Delegates.{1}) GetAddress(\"{1}\", typeof({0}.Delegates.{1}));",
                            Properties.Bind.Default.OutputTaoClass,
                            f.Name);*/
                }
                sw.WriteLine("                #endregion");
                sw.WriteLine("            }");
            }

            #endregion

            sw.WriteLine("        }");
            sw.WriteLine("        #endregion");
            sw.WriteLine();
        }
        #endregion

        #region Write GetAddress
        private static void WriteGetAddress(StreamWriter sw)
        {
            sw.WriteLine("        public static Delegate GetAddress(string s, Type function_signature)");
            sw.WriteLine("        {");
            sw.WriteLine("            IntPtr address = Tao.OpenGl.GlExtensionLoader.GetProcAddress(s);");
            sw.WriteLine("            if (address == IntPtr.Zero)");
            sw.WriteLine("                return null;");
            sw.WriteLine("            else");
            sw.WriteLine("                return Marshal.GetDelegateForFunctionPointer(address, function_signature);");
            sw.WriteLine("        }");

        }
        #endregion

        #region Write wrappers

        public static void WriteWrappers(StreamWriter sw, List<Function> wrappers)
        {
            sw.WriteLine("        #region Wrappers");
            sw.WriteLine();

            foreach (Function f in wrappers)
            {
                // Hack! Should implement these in the future.
                if (f.Extension)
                    continue;

                if (f.Parameters.ToString().Contains("out IntPtr"))
                    continue;

                if (f.Parameters.ToString().Contains("IntPtr[]"))
                    continue;

                sw.WriteLine("        #region {0}", f.Name.TrimEnd('_'));
                
                if (f.WrapperType == WrapperTypes.ReturnsString)
                {
                    sw.WriteLine("        public static {0} {1}{2}", "string", f.Name.TrimEnd('_'), f.Parameters.ToString());
                    sw.WriteLine("        {");
                    sw.WriteLine("             return Marshal.PtrToStringAnsi({0});", f.CallString());
                    sw.WriteLine("        }");
                }
                else if (f.Name.Contains("glLineStipple"))
                {
                    sw.WriteLine("        public static {0} {1}{2}", f.ReturnValue, f.Name.TrimEnd('_'), f.Parameters.ToString().Replace("GLushort", "GLint"));
                    sw.WriteLine("        {");
                    sw.WriteLine("             glLineStipple_({0}, unchecked((GLushort){1}));", f.Parameters[0].Name, f.Parameters[1].Name);
                    sw.WriteLine("        }");
                }
                else if (f.WrapperType == WrapperTypes.VoidPointerIn || f.WrapperType == WrapperTypes.VoidPointerOut || f.WrapperType == WrapperTypes.ArrayIn)
                {
                    // Add object overload (i.e. turn off type checking).
                    sw.WriteLine("        public static {0} {1}{2}", f.ReturnValue, f.Name.TrimEnd('_'), f.Parameters.ToString().Replace("IntPtr", "object"));
                    sw.WriteLine("        {");
                    int i = 0;
                    StringBuilder sb = new StringBuilder();
                    sb.Append("(");
                    foreach (Parameter p in f.Parameters)
                    {
                        if (p.Type == "IntPtr")
                        {
                            sw.WriteLine("            GCHandle h{0} = GCHandle.Alloc({1}, GCHandleType.Pinned);", i, p.Name);
                            sb.Append("h" + i + ".AddrOfPinnedObject()" + ", ");
                            i++;
                        }
                        else
                        {
                            sb.Append(p.Name + ", ");
                        }
                    }
                    sb.Replace(", ", ")", sb.Length - 2, 2);

                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    if (f.ReturnValue == "void")
                        sw.WriteLine("                {0}{1};", f.Name, sb.ToString());
                    else
                        sw.WriteLine("                return {0}{1};", f.Name, sb.ToString());
                    sw.WriteLine("            }");
                    sw.WriteLine("            finally");
                    sw.WriteLine("            {");
                    while (i > 0)
                    {
                        sw.WriteLine("                h{0}.Free();", --i);
                    }
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");

                    // Add IntPtr overload.
                    sw.WriteLine("        public static {0} {1}{2}", f.ReturnValue, f.Name.TrimEnd('_'), f.Parameters.ToString());
                    sw.WriteLine("        {");
                    sb.Replace(", ", ")", sb.Length - 2, 2);
                    if (f.ReturnValue == "void")
                        sw.WriteLine("            {0};", f.CallString());
                    else
                        sw.WriteLine("            return {0};", f.CallString());
                    sw.WriteLine("        }");
                }
                
                if (f.WrapperType == WrapperTypes.ArrayIn)
                {
                    // Add overload for the case the normal type is used (e.g. float[], bool[] etc).
                    StringBuilder sb = new StringBuilder();
                    sb.Append("(");
                    foreach (Parameter p in f.Parameters)
                    {
                        if (p.Type == "IntPtr")
                        {
                            //sb.Append("[MarshalAs(UnmanagedType.LPArray)] ");
                            sb.Append(p.PreviousType);
                            sb.Append("[] ");
                            sb.Append(p.Name);
                        }
                        else
                            sb.Append(p.ToString());

                        sb.Append(", ");
                    }
                    sb.Replace(", ", ")", sb.Length - 2, 2);
                    sw.WriteLine("        public static {0} {1}{2}", f.ReturnValue, f.Name.TrimEnd('_'), sb.ToString());
                    sw.WriteLine("        {");
                    int i = 0;
                    sb = new StringBuilder();
                    sb.Append("(");
                    foreach (Parameter p in f.Parameters)
                    {
                        if (p.Type == "IntPtr")
                        {
                            sw.WriteLine("            GCHandle h{0} = GCHandle.Alloc({1}, GCHandleType.Pinned);", i, p.Name);
                            sb.Append("h" + i + ".AddrOfPinnedObject()" + ", ");
                            i++;
                        }
                        else
                        {
                            sb.Append(p.Name + ", ");
                        }
                    }
                    sb.Replace(", ", ")", sb.Length - 2, 2);

                    sw.WriteLine("            try");
                    sw.WriteLine("            {");
                    if (f.ReturnValue == "void")
                        sw.WriteLine("                {0}{1};", f.Name, sb.ToString());
                    else
                        sw.WriteLine("                return {0}{1};", f.Name, sb.ToString());
                    sw.WriteLine("            }");
                    sw.WriteLine("            finally");
                    sw.WriteLine("            {");
                    while (i > 0)
                    {
                        sw.WriteLine("                h{0}.Free();", --i);
                    }
                    sw.WriteLine("            }");
                    sw.WriteLine("        }");
                }


                sw.WriteLine("        #endregion");
                sw.WriteLine();
            }
            sw.WriteLine("    #endregion");
            sw.WriteLine();
        }
        #endregion

        #region IsImport
        private static bool IsImportFunction(Function f, string[] import_list)
        {
            if (f.Extension)
                return false;

            foreach (string version in import_list)
                if (f.Version == version)
                    return true;

            return false;
        }
        #endregion
    }
}