using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace Kobalt.Shared.DatabaseConverters;

public class OffsetConverter : ValueConverter<Offset, int>
{
    public OffsetConverter()
        : base(offset => offset.Seconds / 3600, hours => Offset.FromHours(hours)) {}
}

public class NullableOffsetConverter : ValueConverter<Offset?, int?>
{
    public NullableOffsetConverter()
        : base(
            offset => offset.HasValue ? offset.Value.Seconds / 3600 : default,
            hours => hours.HasValue ? Offset.FromHours(hours.Value) : default
            ) {}
}