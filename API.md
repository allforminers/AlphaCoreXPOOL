AlphaxCore exposes a simple REST API at http://localhost:4000. The primary purpose of the API is to power custom web-frontends for the pool.


## Table of Contents

* [/api/pools](#api-pools)
* [/api/pools/&lt;poolid&gt;/blocks](#api-pools-blocks)
* [/api/pools/&lt;poolid&gt;/payments](#api-pools-payments)
* [/api/pools/&lt;poolid&gt;/miners](#api-pools-miners)
* [/api/pools/&lt;poolid&gt;/performance](#api-pools-performance)
* [/api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;](#api-pools-miners-summary)
* [/api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;/payments](#api-pools-miners-payments)
* [/api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;/performance](#api-pools-miners-performance)


<a id="api-pools"></a>
### /api/pools

Returns configuration data and current stats for all configured pools.

Example Response:

```javascript
{
  "pools": [{
      "id": "xmr1",
      "coin": {
        "type": "XMR"
      },
      "ports": {
        "4032": {
          "difficulty": 1600.0,
          "varDiff": {
            "minDiff": 1600.0,
            "maxDiff": 160000.0,
            "targetTime": 15.0,
            "retargetTime": 90.0,
            "variancePercent": 30.0
          }
        },
        "4256": {
          "difficulty": 5000.0
        }
      },
      "paymentProcessing": {
        "enabled": true,
        "minimumPayment": 0.01,
        "payoutScheme": "PPLNS",
        "payoutSchemeConfig": {
          "factor": 2.0
        },
        "minimumPaymentToPaymentId": 5.0
      },
      "banning": {
        "enabled": true,
        "checkThreshold": 50,
        "invalidPercent": 50.0,
        "time": 600
      },
      "clientConnectionTimeout": 600,
      "jobRebroadcastTimeout": 55,
      "blockRefreshInterval": 1000,
      "poolFeePercent": 0.0,
      "address": "9wviCeWe2D8XS82k2ovp5EUYLzBt9pYNW2LXUFsZiv8S3Mt21FZ5qQaAroko1enzw3eGr9qC7X1D7Geoo2RrAotYPwq9Gm8",
      "addressInfoLink": "https://explorer.zcha.in/accounts/9wviCeWe2D8XS82k2ovp5EUYLzBt9pYNW2LXUFsZiv8S3Mt21FZ5qQaAroko1enzw3eGr9qC7X1D7Geoo2RrAotYPwq9Gm8",      
      "poolStats": {
        "connectedMiners": 0,
        "poolHashRate": 0.0
      },
      "networkStats": {
        "networkType": "Test",
        "networkHashRate": 39.05,
        "networkDifficulty": 2343.0,
        "lastNetworkBlockTime": "2017-09-17T10:35:55.0394813Z",
        "blockHeight": 157,
        "connectedPeers": 2,
        "rewardType": "POW"
      }
    }]
}
```

<a id="api-pools-performance"></a>
### /api/pools/&lt;poolid&gt;/performance

Returns pool performance stats for the last 24 hours.

Example Response:

```javascript
{
  "stats": [
    {
      "poolHashRate": 20.0,
      "connectedMiners": 12,
      "created": "2017-09-16T10:00:00"
    },
    {
      "poolHashRate": 25.0,
      "connectedMiners": 15,
      "created": "2017-09-16T11:00:00"
    },

    ...

    {
      "poolHashRate": 23.0,
      "connectedMiners": 13,
      "created": "2017-09-17T10:00:00"
    }
  ]
}

```
<a id="api-pools-miners"></a>
### /api/pools/&lt;poolid&gt;/miners

Returns a pool's top miners by hashrate for the last 24 hours.

<a id="api-pools-blocks"></a>
### /api/pools/&lt;poolid&gt;/blocks

Returns information about blocks mined by the pool. Results can be paged by using the <code>page</code> and <code>pageSize</code> query parameters. Note: transactionConfirmationData is usually the blockchain transaction id.

http://localhost:4000/api/pools/xmr1/blocks?page=2&pageSize=3

Example Response:

```javascript
[
  {
    "blockHeight": 197,
    "status": "pending",
    "effort": 1.4,
    "confirmationProgress": 0.3,
    "transactionConfirmationData": "6e7f68c7891e0f2fdbfd0086d88be3b0d57f1d8f4e1cb78ddc509506e312d94d",
    "reward": 17.558881241740,
    "infoLink": "https://xmrchain.net/block/6e7f68c7891e0f2fdbfd0086d88be3b0d57f1d8f4e1cb78ddc509506e312d94d",
    "created": "2017-09-16T07:41:50.242856"
  },
  {
    "blockHeight": 196,
    "status": "confirmed",
    "effort": 0.85,
    "confirmationProgress": 1,
    "transactionConfirmationData": "bb0b42b4936cfa210da7308938ad6d2d34c5339d45b61c750c1e0be2475ec039",
    "reward": 17.558898015821,
    "infoLink": "https://xmrchain.net/block/bb0b42b4936cfa210da7308938ad6d2d34c5339d45b61c750c1e0be2475ec039",
    "created": "2017-09-16T07:41:39.664172"
  },
  {
    "blockHeight": 195,
    "status": "orphaned",
    "effort": 2.24,
    "confirmationProgress": 0,
    "transactionConfirmationData": "b9b5943b2646ebfd19311da8031c66b164ace54a7f74ff82556213d9b54daaeb",
    "reward": 17.558914789917,
    "infoLink": "https://xmrchain.net/block/b9b5943b2646ebfd19311da8031c66b164ace54a7f74ff82556213d9b54daaeb",
    "created": "2017-09-16T07:41:14.457664"
  }
]
```

<a id="api-pools-payments"></a>
### /api/pools/&lt;poolid&gt;/payments

Returns information about payments issued by the pool. Results can be paged by using the <code>page</code> and <code>pageSize</code> query parameters. Note: transactionConfirmationData is usually the blockchain transaction id.

http://localhost:4000/api/pools/xmr1/payments?page=2&pageSize=3

Example Response:

```javascript
[
  {
    "coin": "XMR",
    "address": "9wviCeWe2D8XS82k2ovp5EUYLzBt9pYNW2LXUFsZiv8S3Mt21FZ5qQaAroko1enzw3eGr9qC7X1D7Geoo2RrAotYPwq9Gm8",
    "addressInfoLink": "https://xmrchain.net/addr/9wviCeWe2D8XS82k2ovp5EUYLzBt9pYNW2LXUFsZiv8S3Mt21FZ5qQaAroko1enzw3eGr9qC7X1D7Geoo2RrAotYPwq9Gm8",
    "amount": 7.5354,
    "transactionConfirmationData": "9e7f68c7891e0f2fdbfd0086d88be3b0d57f1d8f4e1cb78ddc509506e312d94d",
    "transactionInfoLink": "https://xmrchain.net/tx/9e7f68c7891e0f2fdbfd0086d88be3b0d57f1d8f4e1cb78ddc509506e312d94d",
    "created": "2017-09-16T07:41:50.242856"
  }
]
```

<a id="api-pools-miners-summary"></a>
### /api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;

Provides current stats about a miner on a specific pool.

<code>performance.workers</code> is a dictionary where key is a worker name or an empty string for the default worker, and value is an object containing performance metrics for that worker. To compute the combined performance for a miner you need to accumulate the values of all workers in the dictionary.

Example Response:

```javascript
{
  "result": {
    "pendingShares": 16000,
    "pendingBalance": 1.23,
    "totalPaid": 0,
    "lastPayment": null,
    "lastPaymentLink": null,
    "performance": {
      "created": "2017-12-29T13:07:23.845444",
      "workers": {
        "worker1": {
          "hashrate": 1000,
          "sharesPerSecond": 0.0008333333333333334
        },
        "worker2": {
          "hashrate": 2000,
          "sharesPerSecond": 0.001666666
        }
      }
    }
  },
  "success": true,
  "responseMessageType": 0,
  "responseMessageId": null,
  "responseMessageArgs": null
}
```

<a id="api-pools-miners-payments"></a>
### /api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;/payments

Returns information about payments issued by the pool to specified miner. Results can be paged by using the <code>page</code> and <code>pageSize</code> query parameters. Note: transactionConfirmationData is usually the blockchain transaction id.


<a id="api-pools-miners-performance"></a>
### /api/pools/&lt;poolid&gt;/miners/&lt;miner-wallet-address&gt;/performance

Returns miner performance stats for the last 24 hours. The information is broken down into a dictionary entry for each worker.