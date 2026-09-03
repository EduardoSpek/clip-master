using ClipMaster.Domain.Entities;

namespace ClipMaster.Domain.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
