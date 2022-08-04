﻿using PhotoToys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicLanguage;
public class SystemLanguage
{
    public readonly static IReadOnlyList<string> Languages;
    static SystemLanguage()
    {
        Languages = Windows.System.UserProfile.GlobalizationPreferences.Languages;
    }
}
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class DisplayTextAttribute : Attribute
{
    public string Default { get; }
    public string FinalString { get; }
    public DisplayTextAttribute(
        string Default,
        string? USEnglish = null,
        string? UKEnglish = null,
        string? Sinhala = null,
        string? Thai = null
    )
    {
        this.Default = Default;
        string? str = "";
        foreach (var lang in SystemLanguage.Languages)
        {
            str = lang switch
            {
                "en-US" => USEnglish,
                "en-GB" => UKEnglish,
                "si-LK" => Sinhala,
                "th" => Thai,
                _ => null
            };
            if (str != null) goto End;
        }
        str = Default;
    End:
        FinalString = str;
        return;
    }
}
static class Extension
{
    public static string GetDisplayText<T>(this T enumValue) where T : Enum
    {
        try
        {
            var enumType = typeof(T);
            var memberInfos = enumType.GetMember(enumValue.ToString());
            var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
            var valueAttributes = (DisplayTextAttribute)
                enumValueMemberInfo.GetCustomAttributes(
                typeof(DisplayTextAttribute), false)[0];
            return valueAttributes.FinalString;
        }
        catch
        {
            return enumValue.ToString().ToReadableName();
        }
    }
    public static string GetDisplayText<T>(Expression<Func<T, string?>> member)
    {
        var MemberInfo = member.GetMemberInfo();
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.FinalString;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string GetDisplayText<T>(string memberName)
    {
        var MemberInfo = typeof(T).GetMember(memberName)[0];
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.FinalString;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string GetDefaultText<T>(string memberName)
    {
        var MemberInfo = typeof(T).GetMember(memberName)[0];
        try
        {
            var Attr = MemberInfo.GetCustomAttributes<DisplayTextAttribute>(false).First();
            return Attr.Default;
        }
        catch
        {
            return MemberInfo.Name.ToReadableName();
        }
    }
    public static string GetDisplayText<T>()
    {
        try
        {
            var valueAttributes = typeof(T).GetCustomAttributes<DisplayTextAttribute>(false).First();
            return valueAttributes.FinalString;
        }
        catch
        {
            return typeof(T).Name.ToReadableName();
        }
    }
    private static MemberInfo GetMemberInfo<TModel, TItem>(this Expression<Func<TModel, TItem>> expr)
    {
        return ((MemberExpression)expr.Body).Member;
    }

}