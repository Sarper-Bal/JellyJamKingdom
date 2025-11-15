/*
 * SPAWN EVENT DRAWER (GÜNCELLENDİ - v2.0 Data-Driven)
 *
 * * DEĞİŞİKLİKLER (v2.0):
 * - 'enemyPrefabProp' araması, 'enemyDataProp' ('enemyDataToSpawn')
 * araması ile DEĞİŞTİRİLDİ.
 * - 'OnGUI' metodu artık 'enemyPrefab' yerine 'enemyDataToSpawn'
 * alanını çiziyor.
 * - Yükseklik hesabı (GetPropertyHeight) değişmedi, çünkü sadece
 * bir alanı diğeriyle değiştirdik (1 satıra karşılık 1 satır).
 */

using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SpawnEvent))]
public class SpawnEventDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        position.height = lineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
        
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        position.y += lineHeight + spacing;

        // --- Gerekli Özellikleri Bul ---
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // "enemyPrefab" yerine "enemyDataToSpawn" alanını arıyoruz.
        SerializedProperty enemyDataProp = property.FindPropertyRelative("enemyDataToSpawn");
        // SerializedProperty enemyPrefabProp = property.FindPropertyRelative("enemyPrefab"); // <-- SİLİNDİ
        // --- DEĞİŞİKLİK SONU ---
        
        SerializedProperty spawnPointIDProp = property.FindPropertyRelative("spawnPointID");
        SerializedProperty pathIDProp = property.FindPropertyRelative("pathID"); 
        SerializedProperty triggerTimeProp = property.FindPropertyRelative("triggerTime");
        SerializedProperty isPeriodicProp = property.FindPropertyRelative("isPeriodic");
        SerializedProperty repeatIntervalProp = property.FindPropertyRelative("repeatInterval");
        SerializedProperty hasFiniteDurationProp = property.FindPropertyRelative("hasFiniteDuration");
        SerializedProperty endTimeProp = property.FindPropertyRelative("endTime");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty spawnIntervalProp = property.FindPropertyRelative("spawnInterval");
        
        // --- Özellikleri Sırayla Çiz ---
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        EditorGUI.PropertyField(position, enemyDataProp); // <-- DEĞİŞTİ
        // EditorGUI.PropertyField(position, enemyPrefabProp); // <-- SİLİNDİ
        // --- DEĞİŞİKLİK SONU ---
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, spawnPointIDProp);
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, pathIDProp); 
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, triggerTimeProp);
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, countProp);
        position.y += lineHeight + spacing;
        
        EditorGUI.PropertyField(position, spawnIntervalProp);
        position.y += lineHeight + spacing;
        
        // --- Koşullu Çizim (isPeriodic) ---
        EditorGUI.PropertyField(position, isPeriodicProp);
        position.y += lineHeight + spacing;

        bool isPeriodic = isPeriodicProp.boolValue;
        if (isPeriodic)
        {
            EditorGUI.indentLevel++; 
            EditorGUI.PropertyField(position, repeatIntervalProp);
            position.y += lineHeight + spacing;
            EditorGUI.PropertyField(position, hasFiniteDurationProp);
            position.y += lineHeight + spacing;

            bool hasFiniteDuration = hasFiniteDurationProp.boolValue;
            if (hasFiniteDuration)
            {
                EditorGUI.PropertyField(position, endTimeProp);
                position.y += lineHeight + spacing;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    // Yükseklik hesabı (GetPropertyHeight) değişmedi,
    // çünkü 1 satırlık 'enemyPrefab' alanını, 1 satırlık
    // 'enemyDataToSpawn' alanı ile değiştirdik.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float totalHeight = 0;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        totalHeight += lineHeight + spacing; 
        
        // enemyData, spawnPointID, pathID, triggerTime,
        // count, spawnInterval, isPeriodic (Hala TOPLAM 7 SATIR)
        totalHeight += (lineHeight + spacing) * 7; 

        // Koşullu satırlar
        SerializedProperty isPeriodicProp = property.FindPropertyRelative("isPeriodic");
        if (isPeriodicProp.boolValue)
        {
            totalHeight += (lineHeight + spacing) * 2;
            
            SerializedProperty hasFiniteDurationProp = property.FindPropertyRelative("hasFiniteDuration");
            if (hasFiniteDurationProp.boolValue)
            {
                totalHeight += lineHeight + spacing;
            }
        }
        
        return totalHeight;
    }
}