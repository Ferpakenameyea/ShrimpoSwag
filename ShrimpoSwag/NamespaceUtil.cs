internal static class NamespaceUtil
{
    public static string GetNameSpaceDeclaration(string @namespace)
    {
        if (@namespace == "<global namespace>")
        {
            return "";
        }

        return $"namespace {@namespace}";
    }

    public static bool NamespaceNeedsUsing(string @namespace)
    {
        return
            !string.IsNullOrEmpty(@namespace) &&
            !@namespace.StartsWith("System") &&
            @namespace != "<global namespace>";
    }
}