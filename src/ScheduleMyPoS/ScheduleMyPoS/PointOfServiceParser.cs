using ScheduleMyPoS.Models;

namespace ScheduleMyPoS;

internal static class PointOfServiceParser
{
    private const string CsvDelimiter = ",";

    public static PointOfServiceInput Parse(string csvFilePath)
    {
        var pointsOfService = new List<PointOfService>();
        var visitsLeft = new Dictionary<string, int>();
        using var reader = new StreamReader(csvFilePath);

        // Skip the first line (headers)
        reader.ReadLine();

        while (reader.ReadLine() is { } line)
        {
            var fields = line.Split(CsvDelimiter);

            if (fields.Length != 6)
                throw new InvalidDataException("Invalid number of fields");

            var id = fields[0];

            if(!Enum.TryParse<SellingPointCategory>(fields[4], true, out var category))
                throw new InvalidDataException($"Invalid Category: {fields[4]}");

            pointsOfService.Add(new PointOfService(id, category, new District(fields[1], fields[2]), fields[3]));

            if(!int.TryParse(fields[5], out var visits))
                throw new InvalidDataException($"Invalid number of visits: {fields[5]}");

            visitsLeft.Add(id, visits);
        }

        return new PointOfServiceInput(pointsOfService, visitsLeft);
    }
}