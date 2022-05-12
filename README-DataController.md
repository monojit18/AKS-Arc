# Connected Micro-services with Azure Arc using Kubernetes and SQL Managed Instance

## Introduction

Azure Arc provides a simplified *Governance* and *Management* capability by delivering a consistent multi-cloud and on-premises management platform. Azure Arc-enabled Kubernetes allows us to on-board and manage Kubernetes clusters running anywhere -  clusters running on other public cloud providers (*GCP* or *AWS*) or clusters running on on-premise data-centers (*VMware vSphere* or *Azure Stack HCI*) to Azure Arc. Azure Arc-enabled data services allows us to run Azure data services like *SQL Managed Instance* or *PostgreSQL Hyperscale (preview)* anywhere using Kubernetes.

## What this Article does?

In this article we will build an end-to-end flow of a connected set of simple micro-services and an SQL Managed Instance all deployed on an AKS cluster. The same example can be extended to deploy all the components onto any other cluster of choice - GKE, EKS or even on any un-managed cluster.

Following are the components to be deployed:

- **An Azure Function App** - This implemented the core business logic of Add, Update or Fetch from SQL MI database
- **A Logic App** - This implements the notification flow by sending emails to intended recipients and is triggered by the Function App
- **An SQL MI** - This holds the business data needed for the entire flow and can be accessed only from the above applications running within the cluster

All the above components will be running privately within the AKS cluster and is exposed only through an Ingress Controller. This article would deploy this Ingress Controller as a Public Load Balancer for simplicity; but a more stringent and recommended approach would be make this Ingress Controller as an Internal Load Balancer with private IP and expose it only through an Application Gateway or API Management resource, thus making the InBound access more secure.






