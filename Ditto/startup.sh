#!/bin/bash

basePath=$("pwd")
pidFile="${basePath}/pids.pid"

if [ -f $pidFile ];
then
  echo "$pidFile already exists. Stop the process before attempting to start."
else
  echo -n "" > $pidFile
  cd ${basePath}
  echo "Starting Ditto"
  nohup ./Ditto noprompt >/dev/null 2>&1 &
  echo -n "$! " >> $pidFile
fi