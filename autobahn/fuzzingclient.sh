#! /bin/sh

docker run -it --rm -v "${PWD}/config:/config" -v "${PWD}/reports:/reports" -p 9002:9002 --name fuzzingclient crossbario/autobahn-testsuite wstest -m fuzzingclient -s /config/fuzzingclient.json