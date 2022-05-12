# Connected Micro-services with Azure Arc using Kubernetes and SQL Managed Instance

## Introduction

Azure Arc provides a simplified *Governance* and *Management* capability by delivering a consistent multi-cloud and on-premises management platform. Azure Arc-enabled Kubernetes allows us to on-board and manage Kubernetes clusters running anywhere -  clusters running on other public cloud providers (*GCP* or *AWS*) or clusters running on on-premise data-centers (*VMware vSphere* or *Azure Stack HCI*) to Azure Arc. Azure Arc-enabled data services allows us to run Azure data services like *SQL Managed Instance* or *PostgreSQL Hyperscale (preview)* anywhere using Kubernetes.

## What this Article does?

In this article we will build an end-to-end flow of a connected set of simple micro-services and an SQL Managed Instance all deployed on an AKS cluster. The same example can be extended to deploy all the components onto any other cluster of choice - GKE, EKS or even on any un-managed cluster.

Following are the components to be deployed:

- **An Azure Function App** - This implemented the core business logic of Add, Update or Fetch from SQL MI database
- **A Logic App** - This implements the notification flow by sending emails to intended recipients and is triggered by the Function App
- **An SQL MI** - This holds the business data needed for the entire flow and can be accessed only from the above applications running within the cluster

All the above components will be running Privately within the AKS cluster and is exposed only through an Ingress Controller. This article would deploy this Ingress Controller as a *Public Load Balancer* for simplicity; but a more stringent and recommended approach would be make this Ingress Controller as an *Internal Load Balancer* with private IP and expose it only through an Application Gateway or API Management resource, thus making the InBound access more secure.

## Steps to build this

Following are the steps we would follow as we move on:

- Create a basic AKS Cluster. For simplicity, we would not add any additional security or features in this cluster
- **On-board** the cluster onto Azure Arc
- Deploy **Data Controller extension** for Arc
- Deploy **SQL MI** on Azure Arc
  - Connect and review the deployments
- Connect Data services running inside the cluster using the **Azure Data Studio**
- Deploy an **Azure Function App** as Container onto the AKS cluster
- Deploy **Logic App** as container onto the AKS cluster
- Deploy an **Ingress Controller** to provide access to the application and data services running inside the cluster - we would be using **Nginx** here and configure it as a Public Load Balancer
- Deploy **Ingress routing** within the cluster
  - Application Ingress
  - Data Monitor Ingress
- Test the Application flow end-to-end using **Postman**

## Let us delve into this

### Prerequisites

- An active Azure Subscription
- A Github account (optional)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver15)
- [Visual Studio Code](https://code.visualstudio.com/download)(Optional) or any other preferred IDE

### Let us create the AKS Cluster

- We can create it using [Portal](https://docs.microsoft.com/en-us/azure/aks/learn/quick-kubernetes-deploy-portal) or [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/aks?view=azure-cli-latest#az-aks-create)
- We will be taking the CLI approach

#### Set CLI Variables

```bash
tenantId="<tenantId>"
subscriptionId="<subscriptionId>"
arcResourceGroup="arc-k8s-rg"
aksResourceGroup="aks-k8s-rg"
arcsvcResourceGroup="arc-services-rg"
location="eastus"
clusterName="aks-k8s-cluster"
version=1.22.4
acrName=aksk8sacr
acrId=
aksVnetName=aks-k8s-vnet
aksVnetPrefix="<Address prefix of the Vent to host AKS cluster>"
aksVnetId="<To be set later>"
aksSubnetName=aks-k8s-subnet
aksSubnetPrefix="<Address prefix of the Subnet to host AKS cluster>"
aksSubnetId="<To be set later>"
sysNodeSize="Standard_D8s_v3"
sysNodeCount=3
maxSysPods=30
networkPlugin=azure
networkPolicy=azure
sysNodePoolName=arcsyspool
vmSetType=VirtualMachineScaleSets
addons=monitoring
connectedClusterName="aksarccluster"
```

#### Login to Azure

```bash
az login --tenant $tenantId
```

#### Create Resource groups

```bash
# Resoure group for all Arc-enabled resources
az group create -n $arcResourceGroup -l $location
```

```bash
# Resoure group for all AKS cluster and related resources
az group create -n $aksResourceGroup -l $location
```

```bash
# Resoure group for all Application services
az group create -n $arcsvcResourceGroup -l $location
```

#### Create Virtual Network

```bash
az network vnet create -n $aksVnetName -g $aksResourceGroup --address-prefixes $aksVnetPrefix
aksVnetId=$(az network vnet show -n $aksVnetName -g $aksResourceGroup --query="id" -o tsv)
echo $aksVnetId
```

#### Create Subnet to host the AKS cluster

```bash
az network vnet subnet create -n $aksSubnetName --vnet-name $aksVnetName -g $aksResourceGroup --address-prefixes $aksSubnetPrefix
aksSubnetId=$(az network vnet subnet show -n $aksSubnetName --vnet-name $aksVnetName -g $aksResourceGroup --query="id" -o tsv)
echo $aksSubnetId
```

