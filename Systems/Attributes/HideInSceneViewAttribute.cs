using System;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class HideInSceneViewAttribute : Attribute
{
    public string GroupName { get; }

    public HideInSceneViewAttribute(string groupName = null)
    {
        GroupName = groupName;
    }
}
