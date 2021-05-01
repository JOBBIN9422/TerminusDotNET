#!/bin/bash

# If no dir is specified, fail
if [ -z "${ECHOBOT_DIR}" ]; then
    exit 1
fi

cd $ECHOBOT_DIR

docker cp echobot:echobot/echobot/log/ persistent_files/

docker rm -f echobot