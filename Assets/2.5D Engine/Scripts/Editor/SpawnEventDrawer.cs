/*
 * SPAWN EVENT DRAWER (GÜNCELLENDİ - v1.1)
 *
 * GÖREVİ: 'SpawnEvent' sınıfının Inspector'da nasıl görüneceğini özelleştirir.
 *
 * * DEĞİŞİKLİKLER (v1.1):
 * - 'pathID' alanı eklendi.
 * - 'OnGUI' metodu artık 'pathID'yi bulup 'spawnPointID'nin altına çiziyor.
 * - 'GetPropertyHeight' metodu, 'pathID' için ayrılan ekstra satır
 * yüksekliğini hesaba katacak şekilde güncellendi (6 satırdan 7 satıra).
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
        SerializedProperty enemyPrefabProp = property.FindPropertyRelative("enemyPrefab");
        SerializedProperty spawnPointIDProp = property.FindPropertyRelative("spawnPointID");
        
        // --- DEĞİŞİKLİK BAŞLANGICI ---
        SerializedProperty pathIDProp = property.FindPropertyRelative("pathID"); // YENİ eklendi
        // --- DEĞİŞİKLİK SONU ---
        
        SerializedProperty triggerTimeProp = property.FindPropertyRelative("triggerTime");
        SerializedProperty isPeriodicProp = property.FindPropertyRelative("isPeriodic");
        SerializedProperty repeatIntervalProp = property.FindPropertyRelative("repeatInterval");
        SerializedProperty hasFiniteDurationProp = property.FindPropertyRelative("hasFiniteDuration");
        SerializedProperty endTimeProp = property.FindPropertyRelative("endTime");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty spawnIntervalProp = property.FindPropertyRelative("spawnInterval");
        
        // --- Özellikleri Sırayla Çiz ---
        
        EditorGUI.PropertyField(position, enemyPrefabProp);
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, spawnPointIDProp);
        position.y += lineHeight + spacing;

        // --- DEĞİŞİKLİK BAŞLANGICI ---
        // 'pathID' alanını 'spawnPointID'nin hemen altına çiz
        EditorGUI.PropertyField(position, pathIDProp); 
        position.y += lineHeight + spacing;
        // --- DEĞİŞİKLİK SONU ---

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

    // --- DEĞİŞİKLİK BAŞLANGICI (Yükseklik Hesabı) ---
    // 'GetPropertyHeight' metodu, Inspector'da ne kadar yer
    // ayıracağını hesaplar. 'pathID' için 1 satır daha eklemeliyiz.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float totalHeight = 0;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // 1. Foldout başlığı
        totalHeight += lineHeight + spacing; 
        
        // 2. 'enemyPrefab', 'spawnPointID', 'pathID' (YENİ), 'triggerTime',
        //    'count', 'spawnInterval', 'isPeriodic' (TOPLAM 7 SATIR)
        totalHeight += (lineHeight + spacing) * 7; // <-- BU SATIR 6'dan 7'ye güncellendi

        // 3. Koşullu satırları hesapla
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
    // --- DEĞİŞİKLİK SONU ---
}