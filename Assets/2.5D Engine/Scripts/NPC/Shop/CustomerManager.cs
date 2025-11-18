/*
 * MÜŞTERİ YÖNETİCİSİ (SPAWNER & BRAIN) - v1.2 (Market Bağlantılı)
 * * DEĞİŞİKLİKLER:
 * - 'Initialize' metodu MarketController tipini kullanıyor.
 * - 'SpawnCustomer' metodu, müşteriyi spawn eder etmez 'MarketController.AttendToCustomer()' çağırarak
 * Müşteri/NPC döngüsünü başlatır.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private CustomerData customerData;

    [Header("Tüketim Hedefi")]
    [Tooltip("Müşterilerin gideceği Market.")]
    [SerializeField] private MarketController targetMarket; // <-- MarketController alıyor

    [Header("Trafik")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float spawnInterval = 5.0f;
    [SerializeField] private int maxCustomers = 10;

    private Queue<CustomerAI> customerPool = new Queue<CustomerAI>();
    private List<CustomerAI> activeCustomers = new List<CustomerAI>();
    private Transform poolParent;

    private void Start()
    {
        if (targetMarket == null || customerPrefab == null || customerData == null)
        {
            Debug.LogError("CustomerManager: Eksik referanslar!", this);
            return;
        }
        
        poolParent = new GameObject(name + "_Pool").transform;
        
        // Havuzu önceden doldur (Optimize)
        for (int i = 0; i < maxCustomers; i++)
        {
            CreateNewCustomer();
        }

        // Trafiği Başlat
        StartCoroutine(TrafficRoutine());
    }

    private void CreateNewCustomer()
    {
        GameObject go = Instantiate(customerPrefab, poolParent);
        CustomerAI ai = go.GetComponent<CustomerAI>();
        if (ai != null)
        {
            go.SetActive(false);
            customerPool.Enqueue(ai);
        }
    }

    private IEnumerator TrafficRoutine()
    {
        while (true)
        {
            SpawnCustomer();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCustomer()
    {
        if (customerPool.Count == 0 || activeCustomers.Count >= maxCustomers) return;

        CustomerAI customer = customerPool.Dequeue();
        
        customer.transform.position = transform.position;
        customer.gameObject.SetActive(true);
        
        customer.OnArrivedAtExit += HandleCustomerLeft;
        activeCustomers.Add(customer);

        // Müşteriyi başlat ve Markete gönder
        customer.Initialize(customerData, targetMarket, (exitPoint != null) ? exitPoint : transform);
    }

    private void HandleCustomerArrivedAtShop(CustomerAI customer)
    {
        // Bu metot artık kullanılmıyor, çünkü MüşteriAI'ın kendisi
        // MarketController'a kendini kaydediyor.
    }

    private IEnumerator ShoppingProcess(CustomerAI customer)
    {
        // Bu metot artık kullanılmıyor, mantık MarketController'a ve CustomerManager'a dağıtıldı.
        yield return null;
    }

    private void HandleCustomerLeft(CustomerAI customer)
    {
        // Temizlik
        customer.OnArrivedAtExit -= HandleCustomerLeft;
        activeCustomers.Remove(customer);
        
        // Havuza iade
        customer.gameObject.SetActive(false);
        customerPool.Enqueue(customer);
    }
}