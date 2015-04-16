#!/usr/bin/env python

import roslib
roslib.load_manifest('robina_speech')

import rospy
import json
import socket 
from std_msgs.msg import String
from robina_speech.msg import SpeakCompleted, SpeechDetected, SpeechResult


HOST = "192.168.10.112"
PORT = 9771

#def find_myIP():
#    import netifaces as ni
#    return ni.ifaddresses('eth0')[2][0]['addr']

SELF_IP = "192.168.10.110" #find_myIP()
print SELF_IP
speech_recognized = speech_rejected = speech_detected = speech_detected = speech_hypothesized = speak_completed = object

sock=None

def init_connection():
    global sock
    print 'making connections'
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
    sock.bind((SELF_IP, PORT))
    sock.listen(1)
    conn,addr=sock.accept()
    print 'connected'
    return conn

def main():
    global sock
    conn=None
    rospy.init_node("speech_dispatcher")
    speech_recognized = rospy.Publisher('SpeechRecognized', SpeechResult)
    speech_rejected = rospy.Publisher('SpeakRejected',SpeechResult )
    speech_detected = rospy.Publisher('speech_detected', SpeechDetected)
    speech_hypothesized = rospy.Publisher('speech_hypothesized', SpeechResult)
    speak_completed = rospy.Publisher('SpeakCompleted', SpeakCompleted)
    
    print 'intialized'
    
    conn=init_connection()
    

    while not rospy.is_shutdown():

	    try:	
		data = conn.recv(1024)
	    except socket.timeout:
		continue
	    except:
    		conn=init_connection()
	    	continue
	   
	    print data,'\n'
	    try:
	        message = json.loads(data)
	    except:
		conn=init_connection()
		continue

	    #print message         
	    scope = message['Source']
		    
	    #print scope
	    if scope == "SpeechRecognized":
	        _msg = toSpeechResultMsg(message['Result'])
		speech_recognized.publish(_msg)
	    elif scope == "SpeechRejected":
	        _msg = toSpeechResultMsg(message['Result'])
	        speech_rejected.publish(_msg)
	    elif scope == "SpeechDetected":
	        _msg = toSpeechDetectedMsg(message)
	        speech_detected.publish(_msg)
	    elif scope == "SpeechHypothesized":
	        _msg = toSpeechResultMsg(message['Result'])
	        speech_hypothesized.publish(_msg)
	    elif scope == "SpeakCompleted":
	        _msg = toSpeakCompletedMsg(message)
	        speak_completed.publish(_msg)

def toSpeechResultMsg(obj):
    msg = SpeechResult()
    msg.id=obj["id"]
    msg.command=obj["command"]
    msg.text=obj["text"]
    msg.confidence=float(obj["confidence"])
    msg.position=float(obj["position"])
    msg.position_confidence=float(obj["position_confidence"])
    return msg

def toSpeechDetectedMsg(obj):
    
    msg = SpeechDetected()
    msg.position=float(obj["position"])
    msg.position_confidence=float(obj["position_confidence"])
    return msg

def toSpeakCompletedMsg(obj):
    msg = SpeakCompleted()
    print obj
    msg.success=True
    return msg


main()

