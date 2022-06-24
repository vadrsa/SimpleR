#SimpleR
SimpleR is a stripped down version of SignalR, after removing all the custom protocols from SignalR we are left with a SimpleR library.

Why SimpleR?
SimpleR was created to solve the problem of easily cerating high performing WebSocket server on .NET in cases when the client cannot use SignalR. For example, when the client is an IoT device working with some protocol standard, you don't have control over the protocolo thus can't use SignalR. In those cases you are left with very low level programming API's. SimpleR solves that problem for you by giving you much simpler and familiar API's to work with to bootstrap your high performance WebSocket server development.
