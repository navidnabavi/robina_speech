#!/usr/bin/env python	

HOST="192.168.10.112"

import sys
import json
import socket
import thread
from jsonrpclib.jsonrpc import ServerProxy
from PyQt4 import QtGui

ICON_ON="/home/athome/icons/on.png"
ICON_OFF="/home/athome/icons/off.png"

class SpeechUtil(QtGui.QSystemTrayIcon):
    def __init__(self,parent=None):
	    QtGui.QSystemTrayIcon.__init__(self,QtGui.QIcon(ICON_ON),parent)
	    self.state=True
	    #self.icon=QtGui.QSystemTrayIcon(QtGui.QIcon("/home/icon/on.png"),app)
	    self.menu=QtGui.QMenu(parent)
	    self.exitAction=self.menu.addAction("Exit")
	    self.exitAction.triggered.connect(self.quit_app)
	    self.runAction=self.menu.addAction("Run")
	    self.runAction.triggered.connect(self.turnOn)
	    self.killAction=self.menu.addAction("Kill")
	    self.killAction.triggered.connect(self.turnOff)
	    self.setContextMenu(self.menu)
	    self.show()
	    self.remote_access=ServerProxy("http://"+HOST+":"+str(8585)+"/")
	    self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    	    self.sock.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
	    self.sock.bind(("0.0.0.0",9898))
	    self.running=True
	    print "done"
    def start(self):
	thread.start_new_thread(self.check,())
	print 'started'
	sys.exit(app.exec_())
    def check(self):
	while self.running:
	    data,addr=self.sock.recvfrom(1024)
	    
	    message=json.loads(data)
	    
	    if message['Data']['Result']==True:
    	        if not self.state:
	    	    self.setIconOn(True)
		    self.state=True
	    elif self.state==True:
	        self.setIconOn(False)
	        self.state=False
	
    def turnOn(self):
	self.remote_access.access('run')
	print "on"

    def turnOff(self):
	self.remote_access.access('kill')
	print "off"

    def setIconOn(self,on_off):
	if on_off:
	    self.setIcon(QtGui.QIcon(ICON_ON))
	else: self.setIcon(QtGui.QIcon(ICON_OFF))
    def quit_app(self):
	self.running=False
	exit()
	
app=QtGui.QApplication(sys.argv)
util=SpeechUtil(None)
util.start()
