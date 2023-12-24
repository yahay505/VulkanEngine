namespace SourceGen;

public class DeriveAttribute
{
    
}

public static class SourceGenerationHelper
{
    public const string Attribute = @"
namespace VulkanEngine.gen
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class EnumExtensionsAttribute : System.Attribute
    {
    }
}";
}
