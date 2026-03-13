using UnityEngine;

/// <summary>
/// Спавнит 3D-модель как дочерний объект игрока и скрывает дефолтный куб.
/// Коллайдер и Rigidbody остаются на родителе — модель чисто визуальная.
/// </summary>
public class ModelSwitcher : MonoBehaviour
{
    private GameObject spawnedModel;

    /// <summary>
    /// Вызывается DinosaurInitializer. Спавнит модель из SO.
    /// </summary>
    public void SwitchModel(GameObject modelPrefab, float yOffset = 0f)
    {
        if (modelPrefab == null)
        {
            Debug.LogWarning("ModelSwitcher: modelPrefab пуст — игрок остаётся кубом.");
            return;
        }

        // Удаляем старую модель если была
        if (spawnedModel != null)
            Destroy(spawnedModel);

        // Скрываем ВСЕ существующие рендереры (куб + все дочерние объекты типа головы)
        Renderer[] existingRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in existingRenderers)
        {
            r.enabled = false;
        }

        // Спавним модель как дочерний объект
        spawnedModel = Instantiate(modelPrefab, transform);
        spawnedModel.transform.localPosition = new Vector3(0f, yOffset, 0f);
        spawnedModel.transform.localRotation = Quaternion.identity;

        // Удаляем коллайдеры с модели — физика должна быть ТОЛЬКО на родителе
        Collider[] modelColliders = spawnedModel.GetComponentsInChildren<Collider>();
        foreach (Collider col in modelColliders)
        {
            Destroy(col);
        }

        Debug.Log("Модель загружена: " + modelPrefab.name);
    }

    /// <summary>
    /// Возвращает Animator спавненной модели (для DinosaurAnimator).
    /// </summary>
    public Animator GetModelAnimator()
    {
        if (spawnedModel == null) return null;
        return spawnedModel.GetComponentInChildren<Animator>();
    }
}
