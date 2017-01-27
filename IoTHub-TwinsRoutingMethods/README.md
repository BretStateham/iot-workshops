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

## Index
- [Solution Setup](#solution-setup)
  - [Simulator](#simulator)
  - [Service Application](#service-application)
  - [Configuration](#configuration)
- [Creating an Azure Service Bus](#creating-an-azure-service-bus)
- [Creating an IoT Hub](#creating-an-iot-hub)
  - [Add IoT Hub Endpoint](#add-iot-hub-endpoint)
- [Add Test Device to IoT Hub](#add-test-device-to-iot-hub)
- [Creating Device Simulator](#creating-device-simulator)
- [Create Service Application](#create-service-application)
- [Send Desired Configuration](#send-desired-configuration)
- [Add Desired Property Change Handler](#add-desired-property-change-handler)
- [Create Critical Notification Monitor](#create-critical-notification-monitor)
- [Add Direct Method to Activate Requested Change](#add-direct-method-to-activate-requested-change)
- [Run Solution](#run-solution)
- [Extra Credit - Twin Reported Configuration](#extra-credit-1)
- [Extra Credit - Push notifications with Flow](#extra-credit-2)

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
`Endpoint=sb://{Service Bus Name}.servicebus.windows.net/;SharedAccessKeyName=Listener;SharedAccessKey={Shared Access Key}`
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
4) When the IoT Hub has been successfully created, click the new tile for your IoT Hub in 
the Azure portal to open the blade for the new IoT Hub. 
5) Populate the solutions's *config.yaml* file with required settings from the IoT Hub. 
- In the *Overviewb* blade, copy the **Hostname** value.
- In the solution's *config.yaml* file, paste the *Hostname* value into the **AzureIoTHubConfig** >
**Hostname** field.  The value shoud look like `{IoT Hub Name}.azure-devices.net`.
- In the *Shared access policies* blade, select the *iothubowner* policy. Copy the *Connection strin-primary key*. 
- In the solution's *config.yaml*, paste the connection string into the **AzureIoTHubConfig** > 
**ConnectionString** field.  The connection string should look like: 
`HostName={Iot Hub Name}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey={Shared Access Key}`

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

1) Open the *Simulator* project, right click on the *Program.cs* file and select **Rename**. 
Enter **Simulator.cs** at the prompt and allow Visual Studio to rename the class as well.  

2) Add the following `using` statements at the top:

```C#
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
using Type = Core.Type;
``` 

3) Add the following code to the *Simulator* class:

```C#
private const int DefaultDelay = 5000;
private static DeviceClient _deviceClient;
private static DesiredDeviceTwinConfiguration _twinDesiredProperties;
private static int _messageSendDelay = DefaultDelay;

private static readonly object Locker = new object();
private static Status _propertyChangeStatus = Status.Accepted;

```

4) Add the Following code to the *Main* method:

```C#
const string configFilePath = @"../../../config/config.yaml";
var config = configFilePath.GetIoTConfiguration();
var testDevice = config.DeviceConfigs.First();
var azureConfig = config.AzureIoTHubConfig;

var cts = new CancellationTokenSource();

_deviceClient = DeviceClient.Create(
    azureConfig.Hostname,
    new DeviceAuthenticationWithRegistrySymmetricKey(testDevice.DeviceId, testDevice.Key),
    TransportType.Mqtt);

Console.WriteLine("Press any key to exit.");
Console.ReadLine();
cts.Cancel();
```

This code parses the *config.yaml* file into local varaibles and adds a 
Cancellation Token that will be used to gracefully exit tasks we'll build later. 
Then, it creates a *Device Client* that will be used to interact with the IoT Hub.

5) Add the following method to the *Simulator Class that will initialized the device 
clients connection to the IoT Hub:

```C#
private static async Task Connect(DeviceClient deviceClient)
{
    try
    {
        await deviceClient.OpenAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
```

6) Call the `Connect` method from `Main` just after the *deviceClient* is 
initialized as follows:

```C#
Task.Run(async () =>
    {
        await Connect(_deviceClient);
    }, cts.Token)
.Wait(cts.Token);
```

We'll be adding more methods to this async lambda later, so don't optimixe the 
signiture yet.

7) Next, add the following method to the *Simulator* class:

```C#
private static async Task DataSend(CancellationToken cancellationToken)
{
    while (true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = Guid.NewGuid().ToString();

        $"{DateTime.Now:HH:mm:ss tt} - Sending Data: {payload}".LogMessage(ConsoleColor.White);

        var message = new Message(Encoding.ASCII.GetBytes(payload));
        message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Information.ToString("G"));

        // Send the paylod to IoT Hub.
        await deviceClient.SendEventAsync(message);

        // lock for safe read of _messageSendDelay.
        int delay;
        lock (Locker)
        {
            delay = _messageSendDelay;
        }
        // Pause before next simulated device reading.
        await Task.Delay(delay, cancellationToken);
    }
}
```
This method runs a loop that will send a new GUID payload to the IoT Hub at 
intervals specifiec by the `_messageSendDelay` field. We will leverage the 
lock to ensure a consistent read of the *_messageSendDelay* field.

*Note/Extra Credit:* passing a
`Func<int>` to retrieve the delay value on each loop would likely be prefereable
to enhance testability. 

8) Call the `DataSend` method from `Main` just after the *deviceClient* is initialized, 
as follows: `Task.Run(() => DataSend(_deviceClient, cts.Token), cts.Token);`

The shell of the simulator application is nowcomplete.  We'll be adding a fewcallback 
and event handlers later in the tutorial.

## Create Service Application

1) Open the *Service* project, right click on the *Program.cs* file and select **Rename**. 
Enter **Service.cs** at the prompt and allow Visual Studio to rename the class as well.  

2) Add the following `using` statements at the top of the file:

```C#
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
``` 

3) Add the Following code to the *Main* method:

```C#
const string configFilePath = @"../../../config/config.yaml";
var config = configFilePath.GetIoTConfiguration();
var testDevice = config.DeviceConfigs.First();
var azureConfig = config.AzureIoTHubConfig;
var serviceBusConfig = config.AzureServiceBusConfig;

var cts = new CancellationTokenSource();
var registryManager = RegistryManager.CreateFromConnectionString(azureConfig.ConnectionString);

var consoleReadTask = Task.Run(async () => {
    while (true)
    {
        Console.WriteLine("Enter a value to set device reporting frequency (in MS) or `exit` to terminate: ");

        var userInput = Console.ReadLine();
        var newMessageSendDelayValue = userInput.IsNullOrWhiteSpace() ? "" : userInput?.ToLowerInvariant();
        switch (newMessageSendDelayValue)
        {
            // Allow the user to exit the application gracefully 
            case "exit":
                cts.Cancel();
                return;

            default:
                break;
        }
    }
}, cts.Token);

Task.WaitAll(consoleReadTask);

```

This code parses the configuration file information and sets up the Device Registry Manager. Additionally, 
it creates the shell of a Task to read user input from the Console.

## Send Desired Configuration

Each time a user enters a value in the console, the Service application will relay 
the configuration change request to the device leveraging the IoT Hub's Device Twin 
capabilities.  The Twin is a bi-directional cloud based projection of the device, 
through which configuartion and status information can flow.  

1) In *Service.cs* add the following method that selects the device twin information 
from the IoT Hub and prints the results to the console:

```C#
private static async Task QueryTwinConfiguration(RegistryManager registryManager, string deviceId)
{
    var query = registryManager.CreateQuery($"SELECT * FROM devices WHERE deviceId = '{deviceId}'");
    var results = await query.GetNextAsTwinAsync();

    foreach (var result in results)
    {
        Console.WriteLine();
        $"Config report for: {result.DeviceId}".LogMessage(ConsoleColor.DarkYellow);

        $"Desired deviceTwinConfig: {JsonConvert.SerializeObject(result.Properties.Desired, Formatting.Indented)}"
            .LogMessage(ConsoleColor.Yellow);

        Console.WriteLine();
    }
}
```

This method can be called anytime you wish to display the Device Twin's Desired or Reported 
configuration information.

2)  Next, add a method that will send the desired configuration information to the 
Device Twin:

```C#
private static async Task SendDesiredConfiguration(
    RegistryManager registryManager, 
    string deviceId,
    CancellationToken cancellationToken, 
    string newMessageSendDelayValue)
{
    // Get the latest Device Twin State
    var twin = await registryManager.GetTwinAsync(deviceId, cancellationToken);

    var patch = new
    {
        properties = new
        {
            desired = new
            {
                deviceTwinConfig =
                new DesiredDeviceTwinConfiguration(Guid.NewGuid().ToString(),
                    newMessageSendDelayValue)
            }
        }
    };

    ($"Sending desired configuration change for setting `messageSendDelay` with value: " + 
        "{newMessageSendDelayValue} to deviceId: {deviceId}").LogMessage(ConsoleColor.Green);

    await
        registryManager.UpdateTwinAsync(twin.DeviceId, JsonConvert.SerializeObject(patch), twin.ETag,
            cancellationToken);
    await
        Task.Run(() => QueryTwinConfiguration(registryManager, deviceId), cancellationToken);
}
```

This method 
- Gets the latest Device Twin information
- Defines a new `patch` object used to update the Device Twin
- Updates the Device Twin 
- Logs the current desired configuation to the console.

3) Lastly, add the following line to the *default* case of the `consoleReadTask` task 
in the *Main* method:

```C#
await SendDesiredConfiguration(registryManager, testDevice.DeviceId, cts.Token, newMessageSendDelayValue);
break;
```

## Add Desired Property Change Handler

The next step is to have the Simulator monitor for change requests and stage them for application by 
the Direct Method.  In this process, the device will send a notification to the IoT Hub indicating 
whether or not validation of the configuartion change requst was successful.

1) Add the following method to *Simulator.cs*:

```C#
private static async Task OnDesiredPropertyChanged(TwinCollection desiredproperties, object usercontext)
{
    if (desiredproperties.Contains("deviceTwinConfig"))
    {
        Message message;

        var deviceTwinConfig =
            JsonConvert.DeserializeObject<DesiredDeviceTwinConfiguration>(
                desiredproperties["deviceTwinConfig"].ToString());

        // ignore delay for now. Just want to see if it can be parsed.
        int delay;

        if (int.TryParse(deviceTwinConfig.MessageSendDelay, out delay))
        {
            
        }
        else
        {
            
        }

        await _deviceClient.SendEventAsync(message);
    }
}
```
This handler ensures checks that the Desired Configuration contains our `deviceTwinConfig` object 
and sets up the validation for the requested configuration change.

2) In the validation failure case, the `else` branch, of the above handler add the 
following code:
```C#
_propertyChangeStatus = Status.Rejected;

($"Device Twin Property `messageSendDelay` is set to an illegal value: `{deviceTwinConfig.MessageSendDelay}`, " +
    $"change status is 'rejected'. Sending Critical Notification.").LogMessage(ConsoleColor.Red);

message = new Message(
    Encoding.ASCII.GetBytes(
        $"Parameter messageSendDelay value: `{deviceTwinConfig.MessageSendDelay}`, could not be converted to an Int."));

message.Properties.Add(MessageProperty.Type.ToString("G"), Type.ConfigChage.ToString("G"));
message.Properties.Add(MessageProperty.Status.ToString("G"), Status.Rejected.ToString("G"));
message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Critical.ToString("G"));
```

This code will set the `_propertyChangeStatus` to **Rejected** and create a new **Critical** notification 
to be sent to the IoT Hub.  Since the mesage is flagged with the **Critical** property, the
*IoT Hub*'s routing will kick in and forward the message onto the *Service Bus*.

3) In the validation success case, the `if` branch, of the `OnDesiredPropertyChanged` handler, 
add the following code:

```C#
lock (Locker)
{
    _twinDesiredProperties = deviceTwinConfig;
    _propertyChangeStatus = Status.Pending;
}
message = new Message(
    Encoding.ASCII.GetBytes(
        $"Device accepted messageSendDelay value: `{deviceTwinConfig.MessageSendDelay}`.  Property change is pending call to 'acceptDesiredProperties'.")
);

message.Properties.Add(MessageProperty.Type.ToString("G"), Type.ConfigChage.ToString("G"));
message.Properties.Add(MessageProperty.Status.ToString("G"), Status.Pending.ToString("G"));
message.Properties.Add(MessageProperty.Severity.ToString("G"), Severity.Critical.ToString("G"));
```

This code will lock our synchonizing object, update the *Simulator*'s fields and prepare a 
critical *success* message to be sent to the IoT Hub.  As with the failure case, the **Critical**
message property will be processed by the IoT Hub's routing mechanism for forwaing onto the 
*Service Bus*. 

4) Lastly, register the `OnDesiredPropertyChange` callback handler in *Main*.  Add the following 
line inside the initialization task:

```C#
 // Add Callback for Desired Configuration changes.
await _deviceClient.SetDesiredPropertyUpdateCallback(OnDesiredPropertyChange, null);
```


## Create Critical Notification Monitor

With mesasges marked as **critical** flowing to the the custom *IoT Hub Route*, and onto the 
Service Bus, we now need to write a Service app handler to process them.  The handler will process the 
Service Bus mesasges based on the Status of the and for one's where the desired configuration 
validation succeeded, we'll call a Direct Method on the device to accept the change, making it perminant. 

1)  Add the following code to the *Simulator* class:

```C#
private static void CriticalNotificationMonitor(
    AzureServiceBusConfig serviceBusConfig,
    string iotHubConnectionString,
    string deviceId,
    CancellationToken token)
{
    var client = QueueClient.CreateFromConnectionString(serviceBusConfig.ConnectionString,
        serviceBusConfig.QueueName);

    var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

    client.OnMessage(async message =>
    { 

    });
}
```
The method configures a *Service Bus* client and definces a callback, the lambda in `OnMesage`.
Additionally, it creates a Service Client that will be used to call the Direct Methond on the 
device.

2) In the body of the `OnMessage` call, add the following code:

```C#
    var status = (Status) Enum.Parse(
                    typeof(Status), 
                    (string) message.Properties[MessageProperty.Status.ToString("G")]);
``` 
While the code completion benefits of using `enum`s for state representation are helpful, crossing 
domain boundries with them can be cause for exceptionally verbose code, like the parse statement above.

3) Add a `switch` statement below `status` to process the various message status:

```C#
switch (status)
{
    case Status.Pending:
        break;
    case Status.Rejected:
        break;
    case Status.Accepted:
    case Status.PreconditionFailed:
    default:
        throw new ArgumentException();
}
```

We are primarily interested in messages that have a either **Pending** or **Rejected** 
status; all others can simply result in an `ArgumentException`.

4) For desired configuration values that failed validation, **Rejected** case, we'll log them to 
the console:

```C#
var body = new StreamReader(message.GetBody<Stream>()).ReadToEnd();
$"Setting value was rejected by the device: '{deviceId}' with message: '{body}'. Please enter a new legal value: "
    .LogMessage(ConsoleColor.Red);
break;
```

The code reads the body of the message as a `Stream`

5) If the desired configuration passed validation then we'll use the Service Client to call a 
direct method on the client as follows:

```C#
// Send message to accept state change.
var method = new CloudToDeviceMethod(DeviceMethods.AcceptDesiredProperties.ToString("G"),
    TimeSpan.FromSeconds(30));

var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, method, token);

$"Device responded with result code: {result.Status} and message: {result.GetPayloadAsJson()}"
    .LogMessage(ConsoleColor.Green);
```

The code creates a new CloudToDeviceMethod to call the Direct Method handler bound to **AcceptDesiredProperties**. 
It then uses the Service Client to invoke the Direct Method and reports the result.

6) In the *Service* `Main` method, before the `consoleReadTask`, add the following code to bind the handler:

```C#
CriticalNotificationMonitor(serviceBusConfig, azureConfig.ConnectionString, testDevice.DeviceId, cts.Token);
``` 

## Add Direct Method to Activate Requested Change

We need to now add, and wire up code that can answer calls to the *AcceptDesiredProperties*
Direct Method. 

1) Open the *Simulator* class and add the following method shell to the class:

```C#
private static Task<MethodResponse> OnAcceptDesiredProperty(MethodRequest request, object context)
{
    MethodResponse response;

    if (Monitor.TryEnter(Locker) && (_propertyChangeStatus == Status.Pending))
    {
        // Locker object can be locked.
    }
    else
    {
        // Lock could not be obtained. 
    }
    return
        Task.FromResult(response);
}
```

This method defines a response object that well be sent back as the result of one of 
the following cases:
- Lock object is available and the `_propertyChangeStatus` is set to **Pending**, 
meaning that the change request can be accepted.
- Lock object is not available becuase a configuration change request is being executed 
or the `_propertyChangeStatus` is set to any other value than **Pending**.

2) In the second case, the `else` branch, add the following code:

```C#
// Note: Locked status code should really only be used with precondition header.
// but direct methods do not allow access to headers to check precondition assertions.

const int httpLockedStatusCode = 423;

var lockedResponseMessage = new
{
    AcceptanceRequestDateTime = DateTime.UtcNow,
    Status = "PreconditionFailed",
    Message = "A precondition for acceptance of the desired configuration change, failed."
};

response = new MethodResponse(
    Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(lockedResponseMessage)),
    httpLockedStatusCode
);
```

As noted in the comments, Direct Methods do not expose the origional cloud side 
http request headers for Direct Methods.  Typically, we would want the client, 
the service app, to make an assertion about the required state of the server (Simulator) 
as a precondition for fulfilment of the requst. Despite not being able to leverage 
a [Precondition header] typically associated with a 412 response, will use the 
response code given the fundamental justification stands; the server's state was 
unacceptible for successful processing of the request.

3) In the successful path, the `if` branch, add the following code:

```C#
$"Updating message send delay from {_messageSendDelay} to {_twinDesiredProperties.MessageSendDelay}. Change will take effect immediatly."
                .LogMessage(ConsoleColor.Green);

_messageSendDelay = int.Parse(_twinDesiredProperties.MessageSendDelay);
_propertyChangeStatus = Status.Accepted;
var responseMessage = new
{
  AcceptanceRequestDateTime = DateTime.UtcNow,
  Status = _propertyChangeStatus,
  Message = "'MessageSendDelay' change accepted by device"
};
response = new MethodResponse(
  Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(responseMessage)),
  (int) HttpStatusCode.OK);
```

In this branch, we have aquired a lock to safely modify both the `_propertyChangeStatus` 
and `_messageSendDelay` fields.  Additionally we create a response message for relay 
back to the client, indicating successful application of the desired property. 

3) Now, modify the initialization task in the *Simulator.cs* to register the 
Direct Method handler.  The complete Task should look like the following:

```C#
// execute initial connect synchronously
Task.Run(async () =>
{
    await Connect(_deviceClient);
    await GetInitialDesiredConfiguration(_deviceClient);

    // Add Callback for Desired Configuration changes.
    await _deviceClient.SetDesiredPropertyUpdateCallback(OnDesiredPropertyChanged, null);

    // Add Handler to set property request 
    _deviceClient.SetMethodHandler("AcceptDesiredProperties", OnAcceptDesiredProperty, null);
}
, cts.Token)
.Wait(cts.Token);
``` 

The `OnAcceptDesiredProperty` handler will now be called anytime the *AcceptDesiredProperties*
Direct Method is called.


## Run Solution

- Right Click on the Solution in the Solution Explorer, Select `Set Startup Projects ...`
- Check `Multiple startup projects` and set Service and Simulator to `Start`
- Click *Apply* and *OK*
- Run the Solution 
- Try entering new millisecond values at the prompt, or illegal values like 'moose'.

## Extra Credit 1 - Twin Reported Configuration

- In the *Simulator* application's `OnAcceptDesiredProperty` method, make a change 
to the success branch to update the Device Twin's Reported Configuration with the 
accepted _messageSendDelay value.  Here is some example code to work from:

```C#
TwinCollection reportedProperties = new TwinCollection();
reportedProperties["DateTimeLastUpdated"] = DateTime.UtcNow;

Client.UpdateReportedPropertiesAsync(reportedProperties);
```

- In the *Service* application's `QueryTwinConfiguration` method print the device's
reported configuration element.  In the `CriticalNotificationMonitor` method call 
`QueryTwinConfiguration` if the `serviceClient.InvokeDeviceMethodAsync` returns an 
HTTP status code equal to 200.

## Extra Credit 2 - Push notifications with Flow

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
