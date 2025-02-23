public interface ISettingsStore
{
    bool IsMusicOn();
    void ToggleMusic();

    bool IsSFXOn();
    void ToggleSFX();
}