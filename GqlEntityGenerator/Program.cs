using System.Text.RegularExpressions;

if (args.Length < 2)
{
    Console.WriteLine("Usage: GqlEntityGenerator <schema.graphql> <outputDir>");
    return;
}

string schemaPath = args[0];
string outputDir = args[1];

if (!File.Exists(schemaPath))
{
    Console.WriteLine($"Schema file not found: {schemaPath}");
    return;
}

string schema = File.ReadAllText(schemaPath);
var types = ParseGraphQLTypes(schema);

Directory.CreateDirectory(outputDir);

foreach (var type in types)
{
    string classCode = GenerateCSharpClass(type);
    string filePath = Path.Combine(outputDir, $"{type.Name}.cs");
    File.WriteAllText(filePath, classCode);
    Console.WriteLine($"Generated: {filePath}");
}

// LOCAL FUNCTIONS

List<GqlType> ParseGraphQLTypes(string schema)
{
    var types = new List<GqlType>();
    var typeRegex = new Regex(@"type\s+(\w+)\s*{([^}]*)}", RegexOptions.Multiline);
    var fieldRegex = new Regex(@"(\w+)\s*(?:\([^\)]*\))?\s*:\s*([!\[\]\w]+)");

    foreach (Match typeMatch in typeRegex.Matches(schema))
    {
        var typeName = typeMatch.Groups[1].Value;
        var fieldsBlock = typeMatch.Groups[2].Value;
        var fields = new List<GqlField>();

        foreach (Match fieldMatch in fieldRegex.Matches(fieldsBlock))
        {
            fields.Add(new GqlField
            {
                Name = fieldMatch.Groups[1].Value,
                Type = fieldMatch.Groups[2].Value
            });
        }

        types.Add(new GqlType
        {
            Name = typeName,
            Fields = fields
        });
    }

    return types;
}

string GenerateCSharpClass(GqlType type)
{
    var code = $"public class {type.Name}\n{{\n";
    foreach (var field in type.Fields)
    {
        string csharpType = MapGraphQLTypeToCSharp(field.Type);
        code += $"    public {csharpType} {FirstCharToUpper(field.Name)} {{ get; set; }}\n";
    }
    code += "}\n";
    return code;
}

string MapGraphQLTypeToCSharp(string gqlType)
{
    bool isNonNull = gqlType.EndsWith("!");
    string cleanType = gqlType.TrimEnd('!');

    string csharpType = cleanType switch
    {
        "Int" => "int",
        "Float" => "double",
        "String" => "string",
        "Boolean" => "bool",
        "ID" => "string",
        // Other custom mappings
        "BigDecimal" => "decimal",
        "BigInteger" => "long",
        "Date" => "DateTime",
        "SqlTimestamp" => "DateTime",
        _ when cleanType.StartsWith("[") && cleanType.EndsWith("]") =>
            $"List<{MapGraphQLTypeToCSharp(cleanType.Trim('[', ']'))}>",
        _ => cleanType
    };

    if (!isNonNull && csharpType != "string" && !csharpType.StartsWith("List<"))
        csharpType += "?";

    return csharpType;
}

string FirstCharToUpper(string input)
{
    if (string.IsNullOrEmpty(input))
        return input;
    return input[0].ToString().ToUpper() + input.Substring(1);
}


class GqlType
{
    public string Name { get; set; }
    public List<GqlField> Fields { get; set; }
}

class GqlField
{
    public string Name { get; set; }
    public string Type { get; set; }
}