using Domain.ValueObjects;

namespace Application.DTO
{
    public sealed class AvatarSettingsViewModel
    {
        public float Height { get; set; }
        public float ShoulderWidth { get; set; }
        public float BodyWidth { get; set; }
        public float HeadSize { get; set; }
        public SkinColor SkinColor { get; set; }
        public HairColor HairColor { get; set; }
    }
}
