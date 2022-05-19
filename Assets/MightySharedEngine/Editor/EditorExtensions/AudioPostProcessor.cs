using UnityEngine;
using UnityEditor;

public sealed class AudioPostProcessor : AssetPostprocessor
{
    private const string projectPath = "Assets/Project";

    [System.Serializable]
    public sealed class AudioUserData
    {
        // Never change these variable names without some sort of migration strategy for the JSON stored in the meta userData
        public int Version = 1;
        public bool FirstImportComplete = false;
        public bool HasBeenSetToHighQuality = false;
    }

    void OnPostprocessAudio(AudioClip clip)
    {
        AudioImporter audioImporter = assetImporter as AudioImporter;

        if (!audioImporter.assetPath.StartsWithFast(projectPath)) return;

        AudioUserData audioUserData = GetAudioUserDataForAsset(audioImporter);

        if (audioUserData == null) return;

        if (audioUserData.FirstImportComplete) return;

        audioImporter.loadInBackground = true;
        audioImporter.preloadAudioData = false;

        AudioImporterSampleSettings sampleSettings = audioImporter.defaultSampleSettings;
        sampleSettings.quality = 0.01f;
        audioImporter.defaultSampleSettings = sampleSettings;

        audioUserData.FirstImportComplete = true;
        audioImporter.userData = JsonUtility.ToJson(audioUserData, false);

        audioImporter.SaveAndReimport();
    }

    public static AudioUserData GetAudioUserDataForAsset(AudioImporter audioImporter)
    {
        AudioUserData audioUserData = null;
        try {
            audioUserData = JsonUtility.FromJson<AudioUserData>(audioImporter.userData);
        } catch { }

        if (audioUserData == null && !string.IsNullOrEmpty(audioImporter.userData)) {
            Debug.LogError("[AudioPostProcessor] userData of asset '" + audioImporter.assetPath + "' already used by something else. Please yell at Scott Beca about this error as he didn't account for this use case. YOLODEV!");
            return null;
        }

        if (audioUserData == null) audioUserData = new AudioUserData();

        return audioUserData;
    }
}
