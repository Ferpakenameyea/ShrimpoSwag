using System;
using System.Collections.Generic;
using System.Text;

namespace ShrimpoSwag
{
    internal class ControllerReturnType(int statusCode)
    {
        public int StatusCode { get; } = statusCode;
        public List<Property> Properties { get; } = [];
    }

    internal class Property(string typeName, string name, string? extraUsing = null)
    {
        public string? ExtraUsing { get; } = extraUsing;
        public string TypeName { get; } = typeName;
        public string Name { get; } = name;
    }
}
