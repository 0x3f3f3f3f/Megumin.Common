﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Megumin
{
    /// <summary>
    /// 代码生成器的信息。
    /// </summary>
    /// <remarks>
    /// 也用来表示和区分代码是不是由代码生成器生成的。
    /// </remarks>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class CodeGeneratorInfoAttribute : Attribute
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string CodeGenericBy { get; set; }
        public string SourceFilePath { get; set; }
        public string SourceFileVersion { get; set; }
    }

    /// <summary>
    /// 代码生成器
    /// </summary>
    public abstract class CodeGenerator
    {
        //常用宏
        public const string Namespace = "$(Namespace)";
        public const string ClassName = "$(ClassName)";

        public const string CodeGenericBy = "$(CodeGenericBy)";
        public const string SourceFilePath = "$(CodeGenericSourceFilePath)";
        public const string SourceFileVersion = "$(CodeGenericSourceFileVersion)";


        [Serializable]
        public class Mecro
        {
            public string Name;
            public string Value;
        }

        /// <summary>
        /// 代码生成器版本。
        /// </summary>
        public abstract Version Version { get; }

        /// <summary>
        /// 允许一个宏嵌套另一个宏
        /// </summary>
        public List<Mecro> MecroList { get; set; }


        /// <summary>
        /// 允许一个宏嵌套另一个宏
        /// </summary>
        public Dictionary<string, string> Macro = new();

        /// <summary>
        /// 新行换行符，默认使用 "\n"
        /// </summary>
        public string NewLine = "\n";

        //TODO
        public static Dictionary<string, string> ProjectMacro = new();

        /// <summary>
        /// 递归替换宏。
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string ReplaceMacro(string code)
        {
            if (ContainsMacro(code) == false)
            {
                return code;
            }

            StringBuilder sb = new(code);
            return ReplaceMacro(sb);
        }

        /// <summary>
        /// 递归替换宏。
        /// TODO: 递归处理宏字典，然后在替换codestr，性能应该能更高
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        public string ReplaceMacro(StringBuilder sb)
        {
            if (MecroList != null)
            {
                foreach (var item in MecroList)
                {
                    sb = sb.Replace(item.Name, item.Value);
                }
            }

            foreach (var item in Macro)
            {
                sb = sb.Replace(item.Key, item.Value);
            }

            var result = sb.ToString();
            if (ContainsMacro(result))
            {
                result = ReplaceMacro(sb);
            }

            return result;
        }

        /// <summary>
        /// 代码字符串中是否含有宏
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public bool ContainsMacro(string code)
        {
            if (MecroList != null)
            {
                foreach (var item in MecroList)
                {
                    if (code.Contains(item.Name))
                    {
                        return true;
                    }
                }
            }

            foreach (var item in Macro)
            {
                if (code.Contains(item.Key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否有宏的值是否包含一个宏
        /// </summary>
        /// <returns></returns>
        public bool MacroValueContainsMacro()
        {
            if (MecroList != null)
            {
                foreach (var item in MecroList)
                {
                    if (ContainsMacro(item.Value))
                    {
                        return true;
                    }
                }
            }

            foreach (var item in Macro)
            {
                if (ContainsMacro(item.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public abstract string GetCodeString();

        public virtual void Generate(string path, Encoding encoding = null)
        {
            string txt = GetCodeString();

            StringBuilder stringBuilder = new();

            //插入生成代码文件的注释头
            stringBuilder.Append($"///********************************************************************************************************************************");
            stringBuilder.Append(NewLine);
            stringBuilder.Append($"///The code on this page is generated by the code generator, do not manually modify.");
            stringBuilder.Append(NewLine);
            stringBuilder.Append($"///CodeGenerator: {this.GetType().FullName}.  Version: {Version}");
            stringBuilder.Append(NewLine);

            if (ContainsMacro(CodeGenericBy))
            {
                stringBuilder.Append($"///CodeGenericBy: {CodeGenericBy}");
                stringBuilder.Append(NewLine);
            }

            if (ContainsMacro(SourceFilePath))
            {
                stringBuilder.Append($"///CodeGenericSourceFilePath: {SourceFilePath}");
                stringBuilder.Append(NewLine);
            }

            if (ContainsMacro(SourceFileVersion))
            {
                stringBuilder.Append($"///CodeGenericSourceFileVersion: {SourceFileVersion}");
                stringBuilder.Append(NewLine);
            }

            stringBuilder.Append($"///********************************************************************************************************************************");
            stringBuilder.Append(NewLine);
            stringBuilder.Append(NewLine);
            stringBuilder.Append(txt);

            txt = ReplaceMacro(stringBuilder);

            if (encoding == null)
            {
                encoding = new System.Text.UTF8Encoding(true);
            }

            System.IO.FileInfo file = new(path);
            file.Directory?.Create();// If the directory already exists, this method does nothing.

            File.WriteAllText(path, txt, encoding);
        }

        public virtual void GenerateNear(UnityEngine.Object near, string replaceFileName = null, Encoding encoding = null)
        {
#if UNITY_EDITOR

            if (!ContainsMacro(ClassName))
            {
                Macro[ClassName] = near.name;
            }

            var path = UnityEditor.AssetDatabase.GetAssetPath(near);
            var projectPath = Path.Combine(Application.dataPath, $"..\\");
            var gp = Path.Combine(projectPath, path);
            gp = Path.GetFullPath(gp);

            var finfo = new FileInfo(gp);

            if (!ContainsMacro(CodeGenericBy))
            {
                Macro[CodeGenericBy] = near.GetType().Name;
            }

            if (!ContainsMacro(SourceFilePath))
            {
                Macro[SourceFilePath] = path;
            }

            string fileName = $"{near.name}_GenericCode.cs";
            if (!string.IsNullOrEmpty(replaceFileName))
            {
                fileName = $"{replaceFileName}.cs";
            }

            var fn = Path.Combine(finfo.Directory.ToString(), fileName);
            fn = Path.GetFullPath(fn);

            Generate(fn, encoding);

            UnityEditor.AssetDatabase.Refresh();

#else
            throw new NotSupportedException();
#endif

        }

        public string UpperStartChar(string str, int length = 1)
        {
            if (str.Length >= 1)
            {
                var up = str.Substring(0, length).ToUpper() + str.Substring(length);
                return up;
            }

            return str;
        }
    }

    /// <summary>
    /// 代码生成器
    /// </summary>
    public class CSCodeGenerator : CodeGenerator
    {
        public override Version Version { get; } = new Version(1, 0, 2);

        public static string GetIndentStr(int level = 1)
        {
            string res = "";
            for (int i = 0; i < level; i++)
            {
                res += "    ";
            }
            return res;
        }

        public int Indent { get; internal set; } = 0;
        public List<string> Lines { get; set; } = new List<string>();

        public void Push(string code, int indentOffset = 0)
        {
            if (!string.IsNullOrEmpty(code))
            {
                code = GetIndentStr(Indent + indentOffset) + code;
            }

            Lines.Add(code);
        }

        public void PushLines<T>(T lines, int indentOffset = 0)
            where T : IEnumerable<string>
        {
            foreach (var line in lines)
            {
                Push(line, indentOffset);
            }
        }

        /// <summary>
        /// 添加一个空行
        /// </summary>
        /// <param name="count"></param>
        public void PushBlankLines(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Push("");
            }
        }

        public void PushWrapBlankLines(string code, int count = 1, int indentOffset = 0)
        {
            if (Lines.LastOrDefault() != "")
            {
                PushBlankLines(count);
            }

            Push(code, indentOffset);
            PushBlankLines(count);
        }

        public void Push(CSCodeGenerator generator, int indentOffset = 0)
        {
            foreach (var item in generator.Lines)
            {
                Push(item, indentOffset);
            }
        }

        /// <summary>
        /// 行分隔符
        /// </summary>
        public static readonly string[] NewLineSeparator = new string[] { "\r\n", "\n", "\r" };

        public string[] SplitTemplate(string template)
        {
            return SplitTemplate(template, NewLineSeparator, StringSplitOptions.None);
        }

        public string[] SplitTemplate(string template, string[] separator, StringSplitOptions options = StringSplitOptions.None)
        {
            var lines = template.Split(separator, options);
            return lines;
        }

        /// <summary>
        /// 添加模板代码或者多行代码。
        /// 内部根据<see cref="NewLineSeparator"/>,拆分成单行添加到生成器中。
        /// </summary>
        /// <param name="template"></param>
        public string[] PushTemplate(string template, int indentOffset = 0)
        {
            return PushTemplate(template, NewLineSeparator, StringSplitOptions.None, indentOffset);
        }

        /// <summary>
        /// 添加模板代码或者多行代码。
        /// </summary>
        /// <param name="template"></param>
        public string[] PushTemplate(string template,
                                     string separator,
                                     StringSplitOptions options = StringSplitOptions.None,
                                     int indentOffset = 0)
        {
            var lines = template.Split(separator, options);
            PushLines(lines, indentOffset);
            return lines;
        }

        /// <summary>
        /// 添加模板代码或者多行代码。
        /// </summary>
        /// <param name="template"></param>
        public string[] PushTemplate(string template,
                                     string[] separator,
                                     StringSplitOptions options = StringSplitOptions.None,
                                     int indentOffset = 0)
        {
            var lines = SplitTemplate(template, separator, options);
            PushLines(lines, indentOffset);
            return lines;
        }

        public void PushComment(string comment, int indentOffset = 0)
        {
            if (comment == null || comment.Length == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(comment))
            {
                return;
            }

            //增加注释
            //Push("");  //不要自动添加空行
            Push(@$"/// <summary>", indentOffset);
            Push(@$"/// {comment}", indentOffset);
            Push(@$"/// </summary>", indentOffset);
        }

        public void PushComment(params string[] comments)
        {
            if (comments == null || comments.Length == 0)
            {
                return;
            }

            if (comments.Length == 1 && string.IsNullOrEmpty(comments[0]))
            {
                return;
            }

            //增加注释
            //Push("");  //不要自动添加空行
            Push(@$"/// <summary>");
            foreach (var item in comments)
            {
                StringReader sr = new(item);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    Push(@$"/// <para/> {line}");
                }
            }
            Push(@$"/// </summary>");
        }

        public void BeginScope()
        {
            Push(@$"{{");
            Indent++;
        }

        public void EndScope()
        {
            Indent--;
            Push(@$"}}");
        }

        public void BeginRegion(string region = null)
        {
            var code = @$"#region";

            if (!string.IsNullOrEmpty(region))
            {
                code = @$"#region {region}";
            }

            PushWrapBlankLines(code);
        }

        public void EndRegion()
        {
            PushWrapBlankLines(@$"#endregion");
        }

        /// <summary>
        /// 结束一个区域,附加一个字符，通常用于 "," 或者 ";"
        /// </summary>
        public void EndScopeWith(string str)
        {
            Indent--;
            Push(@$"}}{str}");
        }

        /// <summary>
        /// 结束一个区域并附带 ";"
        /// </summary>
        public void EndScopeWithSemicolon()
        {
            Indent--;
            Push(@$"}};");
        }

        /// <summary>
        /// 结束一个区域并附带 ","
        /// </summary>
        public void EndScopeWithComma()
        {
            Indent--;
            Push(@$"}},");
        }

        /// <summary>
        /// TODO, 使用sb，换行符问题
        /// </summary>
        /// <returns></returns>
        public override string GetCodeString()
        {
            //string txt = "";
            StringBuilder stringBuilder = new();
            foreach (var item in Lines)
            {
                //txt += item + NewLine;
                stringBuilder.Append(item);
                stringBuilder.Append(NewLine);
            }

            if (NewLine != Environment.NewLine)
            {
                stringBuilder.Replace(Environment.NewLine, NewLine);
            }

            var result = stringBuilder.ToString();

            //var v = txt == result;
            return result;
        }

        public class Scope : IDisposable
        {
            CSCodeGenerator g;
            public string EndWith { get; set; }
            public Scope(CSCodeGenerator g)
            {
                this.g = g;
                g.BeginScope();
            }
            public void Dispose()
            {
                g.EndScopeWith(EndWith);
            }
        }

        public class RegionScope : IDisposable
        {
            CSCodeGenerator g;
            public RegionScope(CSCodeGenerator g, string region = null)
            {
                this.g = g;
                g.BeginRegion(region);
            }

            public void Dispose()
            {
                g.EndRegion();
            }
        }

        /// <summary>
        /// 使用using
        /// </summary>
        /// <returns></returns>
        public IDisposable EnterScope()
        {
            return new Scope(this);
        }

        public IDisposable NewScope
        {
            get
            {
                return new Scope(this);
            }
        }

        public Scope GetNewScope(string endWith = null)
        {
            return new Scope(this) { EndWith = endWith };
        }

        public RegionScope GetRegionScope(string region = null)
        {
            return new RegionScope(this, region);
        }
    }

    /// <summary>
    /// 简单的模板代码生成器。没有循环结构。
    /// </summary>
    [Obsolete("use CSCodeGenerator.PushTemplate instead")]
    public class TemplateCodeGenerator : CodeGenerator
    {
        public override Version Version { get; } = new Version(1, 0, 1);

        public TextAsset Template;

        public override string GetCodeString()
        {
            return Template.text;
        }
    }

    public static class CodeGeneratorExtension_B2F1FF890B2949E1B0431530F1D90322
    {
        //[UnityEditor.MenuItem("Tools/Megumin/CodeGenerator Test")]
        public static void Test()
        {
            string result = "";
            result = typeof(List<int>).ToCode();
            result = typeof(int[]).ToCode();

            result.ToString();

            var enum1 = AdditionalCanvasShaderChannels.Tangent;
            var code1 = enum1.ToCode();
            var enum2 = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            var code2 = enum2.ToCode();

            code2.ToString();
        }

        public static string ToCode(this Type type)
        {
            if (type == null)
            {
                return "";
            }
            else if (type == typeof(void))
            {
                return "void";
            }
            else if (type == typeof(bool))
            {
                return "bool";
            }
            else if (type == typeof(byte))
            {
                return "byte";
            }
            else if (type == typeof(sbyte))
            {
                return "sbyte";
            }
            else if (type == typeof(char))
            {
                return "char";
            }
            else if (type == typeof(short))
            {
                return "short";
            }
            else if (type == typeof(ushort))
            {
                return "ushort";
            }
            else if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(uint))
            {
                return "uint";
            }
            else if (type == typeof(long))
            {
                return "long";
            }
            else if (type == typeof(ulong))
            {
                return "ulong";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(double))
            {
                return "double";
            }
            else if (type == typeof(decimal))
            {
                return "decimal";
            }
            else if (type == typeof(string))
            {
                return "string";
            }

            StringBuilder sb = new();
            if (type.IsGenericType)
            {
                Type gd = type.GetGenericTypeDefinition();
                var gdFullName = gd.FullName;
                gdFullName = gdFullName[..^2];
                sb.Append(gdFullName);
                sb.Append('<');
                var g = type.GetGenericArguments();
                for (int i = 0; i < g.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    var gType = g[i];
                    sb.Append(gType.ToCode());
                }
                sb.Append('>');
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                sb.Append(elementType.ToCode());
                sb.Append("[]");
            }
            else
            {
                sb.Append(type.FullName);
            }

            sb.Replace('+', '.');
            return sb.ToString();
        }

        public static string ToCode<T>(this T obj)
        {
            if (obj == null)
            {
                return "null";
            }
            else if (obj is bool boolObj)
            {
                return boolObj ? "true" : "false";
            }
            else if (obj is Type type)
            {
                return type.ToCode();
            }
            else if (obj is string stringObj)
            {
                return $"\"{stringObj}\"";
            }
            else if (obj is float floatObj)
            {
                return $"{floatObj}f";
            }
            else if (obj is Enum)
            {
                //@enum.GetTypeCode();
                return $"(({obj.GetType().FullName}){Convert.ToInt64(obj)})";
            }

            return obj.ToString();
        }

        /// <summary>
        /// 转为合法标识符字符串，变量名或类型名
        /// https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names
        /// </summary>
        /// <returns></returns>
        public static string ToIdentifier(this Type type)
        {
            StringBuilder sb = new(type.Name);
            if (type.IsGenericType)
            {
                var d = type.GetGenericTypeDefinition();
                sb.Clear();
                sb.Append(d.Name);
                var gs = type.GetGenericArguments();
                foreach (var g in gs)
                {
                    sb.Append("_");
                    sb.Append(g.ToIdentifier());
                }
            }

            return sb.ToIdentifier();
        }

        /// <summary>
        /// 转为合法标识符字符串，变量名或类型名
        /// https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names
        /// </summary>
        /// <returns></returns>
        public static string ToIdentifier(this string result)
        {
            StringBuilder sb = new(result);
            return sb.ToIdentifier();
        }

        /// <summary>
        /// 转为合法标识符字符串，变量名或类型名
        /// https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names
        /// </summary>
        /// <returns></returns>
        public static string ToIdentifier(this StringBuilder sb)
        {
            //第一个字符如果是数字，在前面插入下划线
            if (char.IsDigit(sb[0]))
            {
                sb.Insert(0, '_');
            }

            sb.Replace("[]", "Array");
            sb.Replace('&', '_');
            sb.Replace('.', '_');
            sb.Replace('`', '_');
            sb.Replace(' ', '_');
            sb.Replace('-', '_');

            //将其他非法字符替换为下划线。
            var len = sb.Length;
            for (int i = 0; i < len; i++)
            {
                var ch = sb[i];
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    continue;
                }
                else
                {
                    sb.Replace(ch, '_', i, 1);
                }
            }

            return sb.ToString();
        }

        public static string UpperStartChar(this string str, int length = 1)
        {
            if (str.Length <= length)
            {
                return str.ToUpper();
            }
            else if (str.Length > length)
            {
                var up = str[..length].ToUpper() + str[length..];
                return up;
            }

            return str;
        }
    }
}


