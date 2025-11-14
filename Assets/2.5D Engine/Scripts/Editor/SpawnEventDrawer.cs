/*
 * SPAWN EVENT DRAWER (YENİ EDİTÖR SCRİPT'İ)
 * Bu script, 'SpawnEvent' sınıfının Inspector'da nasıl görüneceğini özelleştirir.
 * 'WaveProfile' asset'ini seçtiğinizde bu script çalışır.
 *
 * GÖREVİ:
 * 1. 'isPeriodic' false ise, 'repeatInterval', 'hasFiniteDuration' ve 'endTime' alanlarını gizler.
 * 2. 'isPeriodic' true ise, 'repeatInterval' ve 'hasFiniteDuration'ı gösterir.
 * 3. 'hasFiniteDuration' da true ise, 'endTime' alanını gösterir.
 * Bu, kullanıcının (senin) hata yapmasını engeller.
 */

using UnityEngine;
using UnityEditor; // Editor script'leri için bu kütüphane gereklidir.

// Bu 'PropertyDrawer', 'SpawnEvent' sınıfını hedef alır.
[CustomPropertyDrawer(typeof(SpawnEvent))]
public class SpawnEventDrawer : PropertyDrawer
{
    // Inspector'da her bir 'SpawnEvent' elemanını çizmek için bu metot çağrılır.
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 'EditorGUI.BeginProperty' ve 'EndProperty' bu özel çizimin
        // prefab'lar ve 'undo' (geri alma) işlemleriyle düzgün çalışmasını sağlar.
        EditorGUI.BeginProperty(position, label, property);

        // 'position' (dikdörtgen), bize ayrılan toplam alanı temsil eder.
        // Biz tek tek alanlar (rect) oluşturup alt alta dizeceğiz.
        // 'lineHeight' bir satırın standart yüksekliğidir.
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing; // Satırlar arası standart boşluk (genelde 2px)
        
        // 'label' (örn: "Element 0") ile başla ve onu 'Foldout' (açılır-kapanır) yap
        position.height = lineHeight; // Yüksekliği tek satıra ayarla
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
        
        // Eğer 'Foldout' kapalıysa (isExpanded == false), hiçbir şey çizme ve bitir.
        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        // --- Foldout AÇIK ise, tüm özellikleri çiz ---

        // İçeriği biraz sağdan başlatmak için girinti (indent) ekle
        EditorGUI.indentLevel++;

        // 'position' dikdörtgenini bir sonraki satıra kaydır
        position.y += lineHeight + spacing;

        // --- Gerekli Özellikleri (Property) 'string' isimleriyle bul ---
        SerializedProperty enemyPrefabProp = property.FindPropertyRelative("enemyPrefab");
        SerializedProperty spawnPointIDProp = property.FindPropertyRelative("spawnPointID");
        SerializedProperty triggerTimeProp = property.FindPropertyRelative("triggerTime");
        SerializedProperty isPeriodicProp = property.FindPropertyRelative("isPeriodic");
        SerializedProperty repeatIntervalProp = property.FindPropertyRelative("repeatInterval");
        SerializedProperty hasFiniteDurationProp = property.FindPropertyRelative("hasFiniteDuration");
        SerializedProperty endTimeProp = property.FindPropertyRelative("endTime");
        SerializedProperty countProp = property.FindPropertyRelative("count");
        SerializedProperty spawnIntervalProp = property.FindPropertyRelative("spawnInterval");
        
        // --- Özellikleri Sırayla Çiz ---
        
        // 'position.height = lineHeight;' her çizimden önce satır yüksekliğini ayarlar.

        EditorGUI.PropertyField(position, enemyPrefabProp);
        position.y += lineHeight + spacing; // Bir sonraki satıra geç

        EditorGUI.PropertyField(position, spawnPointIDProp);
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, triggerTimeProp);
        position.y += lineHeight + spacing;

        EditorGUI.PropertyField(position, countProp);
        position.y += lineHeight + spacing;
        
        // 'spawnInterval'i sadece 'count' 1'den büyükse göstermek mantıklı olabilir,
        // ama şimdilik her zaman gösterelim (basitlik için).
        EditorGUI.PropertyField(position, spawnIntervalProp);
        position.y += lineHeight + spacing;
        
        // --- Koşullu Çizim (İsteğinizin yapıldığı yer) ---
        
        EditorGUI.PropertyField(position, isPeriodicProp);
        position.y += lineHeight + spacing;

        // 'isPeriodicProp'un o anki 'bool' değerini al
        bool isPeriodic = isPeriodicProp.boolValue;

        // EĞER periyodik (isPeriodic) İŞARETLİ İSE:
        if (isPeriodic)
        {
            // Girintiyi bir seviye arttır (daha içeriden görünsün)
            EditorGUI.indentLevel++; 
            
            EditorGUI.PropertyField(position, repeatIntervalProp);
            position.y += lineHeight + spacing;
            
            EditorGUI.PropertyField(position, hasFiniteDurationProp);
            position.y += lineHeight + spacing;

            // 'hasFiniteDurationProp'un o anki 'bool' değerini al
            bool hasFiniteDuration = hasFiniteDurationProp.boolValue;
            
            // EĞER 'hasFiniteDuration' da İŞARETLİ İSE:
            if (hasFiniteDuration)
            {
                EditorGUI.PropertyField(position, endTimeProp);
                position.y += lineHeight + spacing;
            }
            
            EditorGUI.indentLevel--; // Girintiyi azalt
        }

        // Girintiyi normale döndür
        EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }

    // Bu metot, 'OnGUI' metodunun ne kadar dikey alana (yüksekliğe)
    // ihtiyaç duyduğunu hesaplar. Bu, listenin düzgün çizilmesi için kritiktir.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Eğer 'Foldout' kapalıysa, sadece başlık satırının yüksekliğini al (1 satır)
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        // --- Foldout AÇIK ise, tüm satırları hesapla ---
        
        float totalHeight = 0;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // 1. Foldout başlığı
        totalHeight += lineHeight + spacing; 
        
        // 2. enemyPrefab, spawnPointID, triggerTime, count, spawnInterval, isPeriodic (6 satır)
        totalHeight += (lineHeight + spacing) * 6;

        // 3. Koşullu satırları hesapla
        SerializedProperty isPeriodicProp = property.FindPropertyRelative("isPeriodic");
        if (isPeriodicProp.boolValue)
        {
            // 'repeatInterval' ve 'hasFiniteDuration' (2 satır)
            totalHeight += (lineHeight + spacing) * 2;
            
            SerializedProperty hasFiniteDurationProp = property.FindPropertyRelative("hasFiniteDuration");
            if (hasFiniteDurationProp.boolValue)
            {
                // 'endTime' (1 satır)
                totalHeight += lineHeight + spacing;
            }
        }
        
        return totalHeight;
    }
}