#!/usr/bin/env python

#import roslib
#from rospkg.environment import ROS_ETC_DIR
#roslib.load_manifest('robina_speech')

import rospy
from jsonrpclib.jsonrpc import ServerProxy
import robina_speech.msg
from robina_speech.msg import SpeakCompleted,SpeechDetected,SpeechResult
from robina_speech.srv import *
import random
#import ros

class REQ:
    pass

speech_server = None
remote_access = None
HOST="192.168.10.112"
PORT=9595
CONFIDENCE=0.6
#SELF="0.0.14.82"
def main():
    rospy.init_node('SpeechServer')
    
    say = rospy.Service('Say', Say, handleSay)
    recognize = rospy.Service('Recognize', Recognize, handleRecognize)
    loadgrammar= rospy.Service('LoadGrammar', LoadGrammar, handleLoadGrammar)
    unloadgrammar= rospy.Service('UnloadGrammar', UnloadGrammar, handleUnloadGrammar)
    unloadallgrammar =rospy.Service('UnloadAllGrammars', UnloadAllGrammars, handleUnloadAllGrammars)
    emulateRecognition = rospy.Service("EmulateRecognize",EmulateRecognition,handleEmulateRecognition)
    remoteAccess= rospy.Service('RemoteAccess', RemoteAccess, handleRemoteAccess)
    print "Node Intialized"
    global speech_server,remote_access
    speech_server = ServerProxy("http://"+HOST+":"+str(PORT)+"/")
    remote_access=ServerProxy("http://"+HOST+":"+str(8585)+"/")
    print "Remote RPC connected"
    rospy.spin()
    

def handleRemote(req):
	return remote_access.access(req.access);
        
def say(text):
    reqq= REQ()
    reqq.text=text
    reqq.async=False
    handleSay(reqq)

def handleSay(req):
    speech_server.Say(req.text)
    if req.async==False :
    	rospy.wait_for_message('/SpeakCompleted',SpeakCompleted)
    return SayResponse()
def handleRecognize(req):
    result=None

    while not rospy.is_shutdown() :
        result=RecognizeOnce(req.scope,req.type,req.params)
	if result.confidence<CONFIDENCE: continue
	if req.verify:
            say('did you say '+result.text)
            if RecognizeYesNo(): break
	    else:say('sorry, please repeat your command')
	else: break
    return RecognizeResponse(result)

def handleLoadGrammar(req):
    result=speech_server.LoadGrammar(req.grammar)
    
    print result
    #result = rospy.wait_for_message('speech_dispatcer/LoadGrammarLoaded',SpeakCompleted )
    return LoadGrammarResponse()

def handleUnloadGrammar(req):
    result=speech_server.UnloadGrammar(req.text)
    #result = rospy.wait_for_message('speech_dispatcer/LoadGrammarLoaded',SpeakCompleted)
    return UnloadGrammarResponse()

def handleUnloadAllGrammars(req):
    result = speech_server.UnloadAllGrammar()
    #result = rospy.wait_for_message('speech_dispatcer/LoadGrammarLoaded',SpeakCompleted)
    return UnloadAllGrammarsResponse()

def handleConfigure(req):
    result = speech_server.Configure(req.text)
    return ConfigureResponse()

def handleRemoteAccess(req):
    result = remote_access.access(req.action)
    return RemoteAccessResponse(result)
def handleEmulateRecognition(req):
    speech_server.EmulateRecognize(req.text.data)
    return EmulateRecognizeResponse()

def RecognizeOnce(scope,type,cmds):
    print 'rec'
    _id=random.randint(0,0xfffffff)
    print'dddd', _id
    speech_server.StartRecognize(_id)
    while True and (not rospy.is_shutdown()): 
        result = rospy.wait_for_message('/SpeechRecognized',SpeechResult)
	print result.id
	if result.id !=_id and result.id!=0: continue # 0 is for the time which in testing phase somthing has been restarted in windows    
        if scope != "":
            a=str(result.command)
            c=a.find("=")
	    if c != -1:
	    	print c
	    	print a[0:c]
            	result.command = a[0:c]
            	result.scope = a[(c+1):len(a)]
            
        if(result.scope != scope):
	    print "."+scope+"."+result.scope+"."
            continue

        if(type=="just"):
            if (result.command in cmds):
		print 'just is ok'
                break
        elif type == "ignore":
	    print 'hell'
            if (result.command in cmds):
                continue
	    else: break
        else: 
	    break
    return result

def RecognizeYesNo():
    s=RecognizeOnce("", "just",['yes','no']).command
    print s
    return s == 'yes'
       
main()
