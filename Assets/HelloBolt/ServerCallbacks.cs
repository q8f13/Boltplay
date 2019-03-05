using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[BoltGlobalBehaviour(BoltNetworkModes.Server)]
public class ServerCallbacks : Bolt.GlobalEventListener
{
    public override void Connected(BoltConnection connection)
    {
        LogEvent log = LogEvent.Create();
        log.Message = string.Format("{0} connected", connection.RemoteEndPoint);
        log.Send();
    }

    public override void Disconnected(BoltConnection connection)
    {
        LogEvent log = LogEvent.Create();
        log.Message = string.Format("{0} disconnected", connection.RemoteEndPoint);
        log.Send();
    }
}
