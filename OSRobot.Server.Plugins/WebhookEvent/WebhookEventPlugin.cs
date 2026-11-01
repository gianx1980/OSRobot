// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Server.Plugins.WebhookEvent;

public class WebhookEventPlugin : IPlugin
{
    public string Id => "WebhookEvent";

    public string Title => Resource.TxtWebhookEvent;

    public EnumPluginType PluginType => EnumPluginType.Event;

    public List<DynamicDataSample> SampleDynamicData
    {
        get
        {
            List<DynamicDataSample> samples = CommonDynamicData.BuildStandardDynamicDataSamples("Webhook event 1");
            samples.Add(new DynamicDataSample(WebhookEvent.RequestBody, Resource.TxtDynDataRequestBody, "{\"order\": 123}"));
            samples.Add(new DynamicDataSample(WebhookEvent.ContentType, Resource.TxtDynDataContentType, "application/json"));
            samples.Add(new DynamicDataSample(WebhookEvent.SourceIp, Resource.TxtDynDataSourceIp, "192.168.1.20"));
            samples.Add(new DynamicDataSample(WebhookEvent.RequestHeaders, Resource.TxtDynDataRequestHeaders, "Name, Value", true));
            samples.Add(new DynamicDataSample(WebhookEvent.QueryParameters, Resource.TxtDynDataQueryParameters, "Name, Value", true));
            samples.Add(new DynamicDataSample(WebhookEvent.JsonPathData, Resource.TxtDynDataJsonPathDataWebhook, "123"));
            samples.Add(new DynamicDataSample(CommonDynamicData.DefaultRecordsetName, Resource.TxtDynDataWebhookRecordset, "Column1, Column2", true));
            return samples;
        }
    }

    public IPluginInstance GetInstance() => new WebhookEvent();

    public IPluginInstanceConfig GetPluginDefaultConfig() => new WebhookEventConfig();

    public EnumOSPlatform SupportedOSPlatforms => EnumOSPlatform.All;
}
