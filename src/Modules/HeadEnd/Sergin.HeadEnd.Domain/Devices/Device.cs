﻿using Sergin.SharedKernel.Domain;

namespace Sergin.HeadEnd.Domain.Devices;

public class Device : AggregateRoot<DeviceIntenralId>
{
    private Device() { }

    public DeviceId DeviceId { get; private set; }
    ///public DeviceModelInternalId ModelId { get; private set; }
    ///public virtual DeviceModel Model { get; private set; }

    ///public static Device Create(DeviceModelInternalId modelId)
    ///{
    ///    return new Device 
    ///    {
    ///        Id = new DeviceIntenralId(Guid.CreateVersion7()),
    ///        ModelId = modelId
    ///    };
    ///}

    public static Device Create(DeviceId deviceId)
    {
        return new Device 
        {
            Id = new DeviceIntenralId(Guid.CreateVersion7()),
            DeviceId = deviceId
        };
    }
}

public sealed record DeviceIntenralId(Guid Value);
public sealed record DeviceId(string Value);
