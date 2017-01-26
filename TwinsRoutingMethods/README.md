# Update IoT Devices Using Device Twins, Direct Methods and Mesasge Routing

## Overview

In this workshop you will explore Azure IoT Hub's Device Twin, Direct Methods
and Routing capabilities.  All three features will be exercised through the 
implementaiton of a Device Simulator and a Service Application. The complete
solution has been provided in the repository's [Completed Solution](/CompletedSolution)
folder in the event that you get suck and need a reference.

### Use Case 

The demo work flow that will be implemented is as follows:
- The Service application will use values entered into it's Console to request 
a change in the Simulator's data delivery rate, via Device Twin.
- The Simulator will listen for Device Twin desired configuration change 
requests, and perform validation on the requested configuration change value.
- The simulator will send a message marked as *critical* to the IoT hub 
indicating the success or failure of the validation step.
- On validation failure the Service applicaton will display a mesage to the 
user.
- On validation success, the Service application will use a Direct Method to 
instruct the Device to apply the desired configuration change perminantly.

### Extra Credit

If you complete the workshop ahead of schedule, there is an [Extra Credit](#12.-extra-credit) 
exercise that will explore the integration of Push Notifications for validation
failures.  


### Technologies

- [Device Twins](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-device-twins) 
- [Direct Methods](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods)
- [Routes](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-endpoints#custom-routing-endpoints) 

### Prerequsites

To complete the workshop you will need the following:
- Microsoft Visual Studio 2015
- An active Azure Account. (If you do not have an account, you can create 
a [free acocunt](http://azure.microsoft.com/pricing/free-trial/) in just a few 
minutes.)

## Solution SetUp

### Simulator
1) Open the [TwinsRoutingMethods solution file](/TwinsRoutingMethods.sln) in Visual Studio.
2) Add a new project for for the Device Simulator. Click **File** > **Add** > 
**New Project...**, Select **Visual C#** > **Windows** > **Console Application**, 
in the *Name* box enter **Simulator**.
3) Add a *Project Reference* to the included *Core* project. Right click on the project 
in the *Solution Explorer*, and Select **Add** > **Reference**.  From the left nav, 
select **Project** > **Solution**, and add the *Core* project.
4) Use Nuget to add **Microsoft.Azure.Devices.Client** package.  Right click on the 
*Simulator* project, select **Manage Nuget Packages...**.  In the dialog, browse for 
the **Microsoft.Azure.Devices.Client** package, and install.  You may need to **Update**
several dependent packages after installation.

### Service Application

1) Add a new project for for the Service Application. Click **File** > **Add** > 
**New Project...**, Select **Visual C#** > **Windows** > **Console Application**, 
in the *Name* box enter **Service**.
2) Add a *Project Reference* to the included *Core* project. Right click on the project 
in the *Solution Explorer*, and Select **Add** > **Reference**.  From the left nav, 
select **Project** > **Solution**, and add the *Core* project.
3) Use Nuget to add the **Microsoft.Azure.Devices.Client** and **WindowsAzure.ServiceBus** 
packages.  Right click on the *Simulator* project, select **Manage Nuget Packages...**.  
In the dialog, browse for both packages, and install.  You may need to **Update** several 
dependent packages after installation.

### Configuration 

1) Create a local config.yaml file.  In the *Solution Explorer*, open the *config* solution 
folder.  Copy and rename *config.default.yaml* -> *config.yaml*.

## Creating an Azure Service Bus

1) Sign into the [Azure Portal](https://portal.azure.com/) 
2) In the Jumpbar, click **New** > **Enterprise Integration** > **Service Bus** 
3) In the *Service Bus* blade, configure your Service Bus, selecting the appropriate
pricing tier.
- In the **Name** box, enter a name for your Service Bus. If the **Name** is valid and 
available, a green check mark appears in the **Name** box.
- Select a [pricing and scale tier](https://azure.microsoft.com/en-us/pricing/details/service-bus/). 
This tutorial does not require a specific tier.  For this workshop, use the Basic tier.
- In **Resource Group**, either create a resource group, or select an existing one. For 
more information, see [Using resource groups to manage your Azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-portal). 
- In **Location**, select the location to host your Service Bus. For this workshop, 
choose your nearest location. 
4) When you have chosen your Service Bus configuartion options, click **Create**.  It can 
take a few minutes for Azure to create your Service Bus.  To check the status you can monitor 
the procress on the Startboard or in the Notification panel.
5) When the Service Bus has been successfully created, click the new tile for your Service Bus in the Azure
portal to open the blade for the new Service Bus.
6) In the **Shared access policies** blade, click the **Add** button and create a new 
policy with the **Listener** claim.  Press **Create**.  After the claim has been created, 
select the **Listener** claim, and in *Policy:Listener* blade the copy the 
*CONNECTION STRING -PRIMARY KEY* value and paste it into the *config.yaml* file 
of your solution under **AzureServiceBusConfig** > **ConnectionString**.  The connection 
string should look like: 
`Endpoint=sb://my-iot-demo.servicebus.windows.net/;SharedAccessKeyName=Listener;SharedAccessKey=******************************=`
7) In the **Queues** blade, click the **Add** button and create a new queue for 
*critical-notifications*.  In the *Name* box, enter `critical-notifications` and press **Create**.
8) Back in the solution's *config.yaml* file, set the **AzureServiceBusConfig** > **QueueName**
field to the same value (`critical-notifications`).


## Creating an IoT Hub

1) In the Jumpbar, click **New** > **Internet of Things** > **IoT Hub**
2) In the *IoT Hub* blade, configure your IoT Hub, selecting the appropriate pricing tier.
- In the **Name** box, enter a name for your IoT HUb. If the **Name** is valid and 
available, a green check mark appears in the **Name** box.
- Select a [pricing and scale tier](https://azure.microsoft.com/en-us/pricing/details/iot-hub/). 
This tutorial does not require a specific tier.  For this workshop, use the Free tier.
- In **Resource Group**, select the resource groupd created in the previous section. For 
more information, see [Using resource groups to manage your Azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-portal). 
- In **Location**, select the location to host your IoT Hub. For this workshop, 
choose the same location used in the previous section. 
3) When you have chosen your IoT Hub configuartions options, click **Create**.  It can 
take a few minutes for Azure to create your IoT Hub.  To check the status you can monitor 
the procress on the Startboard or in the Notification panel.

### Add IoT Hub Endpoint

1) In the *Endpoints* blade of the *IoT Hub*, select **Add**.
2) In the *Add endpoint* blade, configure the new endpoint.
- In the *Name* box, enter a name for the new Endpoint.
- Select an *Endpoint type* of **Service Bus Queue**.
- In the *Service Bus namespace* dropdown, select the previously created Service Bus.
- In the *Service Bus queue* dropdown, select the *critical-notifications* queue.
3) Click **OK** to create the new *Endpoint*

### Add IoT Hub Route

1) In the *Routes* blade of the *IoT Hub*, select **Add**.
2) In the *Create new route* blade, configure the new route.
- In the *Name* box, enter a name for the new route.
- In the *Data source* dropdown, select **Device Messages**. 
- Set the *Endpoint* to the **cirtical-notifications* endpoint created above.
- Make sure the *Enable Rule* toggle is set to **On**.
- In the *Query string* box, enter `severity="Critical"`.
- In the *Test the route* box paste the following JSON:
```JAVASCRIPT
{
  "devicemessage": {
    "appProperties": {
      "severity": "Critical"
    }
  }
}
```
- Press **Run** and ensure the *Result* equals *Match*.
- Press **Save** to create the new Route.

## Add Test Device to IoT Hub

1) Open the *config* > *config.yaml* file.
2) Under the *DeviceConfigs* section of the YAML file, add a unique value for the *DeviceId*
field.
3) Run the CreateDeviceIdentity project. In the Solution Explorer, right click on the 
*CreateDeviceIdentity* project and select**Set as Startup Project**.  Running the solution 
will add a [Device Identity](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted#create-a-device-identity) 
for the test Device to your IoT Hub.  The console application will automatically insert and 
save the device key generated by your IoT Hub, to the soltuion's *config.yaml* file.

## Creating Device Simulator

## Create Service 

## Send Desired Configuration

## Add Desired Property Change Handler

## Create Critical Notification Monitor

## Add Direct Method to Activate Requested Change

## Run Solution

- Right Click on the Solution in the Solution Explorer, Select `Set Startup Projects ...`
- Check `Multiple startup projects` and set Service and Simulator to `Start`
- Click *Apply* and *OK*
- Run the Solution 
- Try entering new millisecond values at the prompt, or illegal values like 'moose'.

## 12. Extra Credit 

- Add a second Service Bus Queue to split *Pending* and *Rejected* notifications 
to discrete queues.
- Recreate the IoT Hub with a non-free pricing tier.
- Add a new endpoint and route to split *Pending* and *Rejected* notifications.
- Modify the `CriticalNotificationMonitor` method of the Service project, adding 
discrete OnMessage Handlers for both *Pending* and *Rejected* queues.
- In the *Rejected* handler add code to post the following JSON object to a URL.
```JAVASCRIPT 
{
    "Type":"ConfigChange",
    "Severity":"Critical",
    "Message":"this is some text"
}
```
- Download the [Microsoft Flow](https://flow.microsoft.com/en-us/) applicaiton 
to your phone and sign in. 
- Go to [Microsoft Flow](https://flow.microsoft.com/en-us/) and sign in with 
the same account used on your phone.
    - Select *My flows*
    - Select *Create a flow from blank*
    - Select *Request* and enter the following JSON Schema:
```JAVASCRIPT 
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "properties": {
    "Type": {
      "type": "string"
    },
    "Severity": {
      "type": "string"
    },
    "Message": {
      "type": "string"
    }
  },
  "required": [
    "Type",
    "Severity",
    "Message"
  ]
}
```
- Add a *New Step*
- Select *Send a push notifications*, enter "Received the following critical notification: ", 
press *Add dynamic content* and Select `Message` to add the *Message* property of the JSON 
object from the Request Body.  
- Save the Flow, copy the generated URL in the *Request* step and use it in the *Rejected*
queue flow in the Service Application. 
- You should now receive push notifications to your phone everytime a bad desired configuration 
value is sent to the simulator.




How to:
    create service bus basic tier with queue name for critical messages
    create iot-hub
    add iot-hub endpoint for critical notifications to forward to service bus queue
    Add route that maps "severity" = "critical" to critical notifications endpoint. 