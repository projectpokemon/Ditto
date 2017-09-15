#!/bin/bash

pidFile='pids.pid'

if [ -f $pidFile ];
then
  pids=`cat ${pidFile}`

  for pid in "${pids[@]}"
  do
    kill $pid
  done

  rm $pidFile
else
  echo "Process file wasn't found. Aborting..."
fi