#!/usr/bin/env bash

DBPATH="/c/ProgramData/CasinoDB/"
mkdir -p $DBPATH
cp ../Database/Casino.mdf $DBPATH
cp ../Database/Casino_log.ldf $DBPATH

if test -f "${DBPATH}Casino.mdf" && test -f "${DBPATH}Casino_log.ldf"; then
	echo "File successfully saved at ${DBPATH}Casino.mdf."
	echo "File successfully saved at ${DBPATH}Casino_log.ldf."
	echo "Installation Succeeded."
else
	echo "Installation Failed."
fi
