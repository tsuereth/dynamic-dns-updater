DynamicDnsUpdater is a silly little application to (1/2) ask public services for the host's current IP address, then (2/2) update configured DNS records as appropriate.

It's intended for environments where an assigned address isn't necessarily static - such as from residential ISPs - and to run on a schedule i.e. through cron or a systemd timer.

The application is designed to be somewhat extensible, e.g. modular-izing "get my IP" services and "update my records" services, for the purpose of _hypothetical_ future maintenance. But no additional extensions are necessarily planned.

Contributions are not expected.
