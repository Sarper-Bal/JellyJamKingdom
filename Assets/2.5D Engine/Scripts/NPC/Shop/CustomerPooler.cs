/*
 * CUSTOMER POOLER - FIXED (Method Name Mismatch Resolved)
 * DÜZELTME:
 * - 'OnSpawnFromPool()' çağrısı 'OnObjectSpawn()' olarak düzeltildi.
 * - SimpleCustomer ve IPooledObject arayüzü ile uyumlu hale getirildi.
 */

using System.Collections.Generic;
using UnityEngine;

public class CustomerPooler : MonoBehaviour
{
    #region Singleton
    public static CustomerPooler Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    #endregion

    private Queue<SimpleCustomer> poolQueue = new Queue<SimpleCustomer>();
    private SimpleCustomer backupPrefab;

    public void RegisterPool(SimpleCustomer prefab, int quantity)
    {
        if (prefab == null) return;
        // Yedek prefab'i sakla (Havuz biterse üretmek için)
        if (backupPrefab == null) backupPrefab = prefab;

        for (int i = 0; i < quantity; i++) CreateAndEnqueue(prefab);
    }

    private SimpleCustomer CreateAndEnqueue(SimpleCustomer prefab)
    {
        SimpleCustomer obj = Instantiate(prefab, transform);
        obj.gameObject.SetActive(false);
        poolQueue.Enqueue(obj);
        return obj;
    }

    public SimpleCustomer GetCustomer(Vector3 position, Quaternion rotation)
    {
        if (poolQueue.Count == 0)
        {
            if (backupPrefab != null)
            {
                // Havuz boşsa yenisini yarat
                CreateAndEnqueue(backupPrefab);
            }
            else
            {
                return null;
            }
        }

        SimpleCustomer customer = poolQueue.Dequeue();
        customer.transform.position = position;
        customer.transform.rotation = rotation;
        customer.gameObject.SetActive(true);
        
        // --- DÜZELTME BURADA ---
        customer.OnObjectSpawn(); // 'OnSpawnFromPool' yerine doğrusu bu.
        // -----------------------
        
        return customer;
    }

    public void ReturnCustomer(SimpleCustomer customer)
    {
        customer.gameObject.SetActive(false);
        poolQueue.Enqueue(customer);
    }
}