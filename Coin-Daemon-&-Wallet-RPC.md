AlphaxCore requires the coin daemons (and wallet RPC for some coins) to be accessible when AlphaxCore runs. Replace with your info within `<>` brackets. Be aware that this page is not complete and might be outdated.

Just a reminder: use **STRONG** password.


## Table of Contents

* [Bitcoin (BTC)](#bitcoin)
* [Litecoin (LTC)](#litecoin)
* [Zcash (ZEC)](#zcash)
* [Monero (XMR)](#monero)
* [Ethereum (ETH)](#ethereum)
* [Ethereum Classic (ETC)](#ethereum-classic)
* [Expanse (EXP)](#expanse)
* [Dash (DASH)](#dash)


<a id="bitcoin"></a>
### Bitcoin

```console
$ bitcoind -server -datadir=<data-folder-path> -rpcuser=<username> -rpcpassword=<password> -port=8333 -rpcport=8332 -rpcbind=0.0.0.0
```


<a id="monero"></a>
### Monero

You will need to run both of the following:

```console
$ monerod --data-dir <data-folder-path> --log-file /dev/null --non-interactive --rpc-bind-ip 127.0.0.1 --out-peers 32 --p2p-bind-ip 0.0.0.0 --p2p-bind-port 18080
```

```console
$ monero-wallet-rpc --daemon-address 127.0.0.1:18081 --wallet-file=<wallet-file-path> --rpc-bind-port 18082 --rpc-bind-ip 127.0.0.1 --rpc-login <username>:<password> --log-level 1
```


<a id="ethereum"></a>
### Ethereum

```console
$ parity --chain mainnet --base-path <data-folder-path> --mode active --no-ui --no-dapps --no-ipc --jsonrpc-interface "0.0.0.0" --jsonrpc-threads 4 --jsonrpc-port 8545 --no-discovery --jsonrpc-apis "eth,net,web3,personal,parity,parity_pubsub,rpc" --author <your-wallet-address> --cache-size 512 --logging info --ws-port 8546 --ws-interface all
```
If you want to run more than one `parity` instance not in a container (e.g. Docker), replace `--no-ipc` for `--ipc-path "/data/ethd1.ipc"` in above command.

<a id="ethereum-classic"></a>
### Ethereum Classic

```console
$ parity --chain classic --base-path <data-folder-path> --mode active --no-ui --no-dapps --no-ipc --jsonrpc-interface "0.0.0.0" --jsonrpc-threads 4 --jsonrpc-port 8645 --no-discovery --jsonrpc-apis "eth,net,web3,personal,parity,parity_pubsub,rpc" --author <your-wallet-address> --cache-size 512 --logging info --ws-port 8646 --ws-interface all
```
If you want to run more than one `parity` instance not in a container (e.g. Docker), replace `--no-ipc` for `--ipc-path "/data/etcd1.ipc"` in above command.


<a id="expanse"></a>
### Expanse

```console
$ parity --chain expanse --base-path <data-folder-path> --mode active --no-ui --no-dapps --no-ipc --jsonrpc-interface "0.0.0.0" --jsonrpc-threads 4 --jsonrpc-port 8745 --no-discovery --jsonrpc-apis "eth,net,web3,personal,parity,parity_pubsub,rpc" --author <your-wallet-address> --cache-size 512 --logging info --ws-port 8746 --ws-interface all
```
If you want to run more than one `parity` instance not in a container (e.g. Docker), replace `--no-ipc` for `--ipc-path "/data/expd1.ipc"` in above command.