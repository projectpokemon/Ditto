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
  nohup ./Watchog Ditto noprompt >>WatchogExecution.log 2>&1 &
  echo -n "$! " >> $pidFile
fi