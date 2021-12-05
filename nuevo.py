from agent import Roomba, BorderAgent, Trashcan, Box
from model import RandomModel
from flask import Flask, request, jsonify
from random import uniform

robotModel = None 
currentStep = 0


# Coordinate server using Flask

app = Flask("Coordinate server")


@app.route('/init', methods=['POST', 'GET'])
def initModel():
    global currentStep, num_agents, width, height, density, maxSteps, nBoxes

    if request.method == 'POST':
        num_agents = int(request.form.get("numAgents"))
        width = int(request.form.get("width"))
        height = int(request.form.get("height"))
        density = int(request.form.get("density"))
        maxSteps = int(request.form.get("maxSteps"))
        nBoxes = int(request.form.get("nBoxes"))
        currentStep = 0
        print(f"Recieved num agentes = {num_agents}")
        # return jsonify({"OK": num_agents})
    
    global robotModel 
    robotModel = RandomModel(num_agents, width, height, density, maxSteps, nBoxes)
    return jsonify({"message":"Parameters recieved, model initiated."})

@app.route('/getAgents', methods=['GET'])
def getAgents():
    global robotModel 

    if request.method == 'GET':
        # roombaPositions = [{"x": x, "y":1, "z":z} for (a, x, z) in robotModel.grid.coord_iter() if isinstance(a, Roomba)]
        roombaPositions = [{"x": agent.pos[0], "y":1, "z":agent.pos[1]} for agent in robotModel.schedule.agents if isinstance(agent, Roomba)]

        return jsonify({'positions':roombaPositions})
#agentes del borde
@app.route('/getObstacles', methods=['GET'])
def getObstacles():
    global robotModel

    if request.method == 'GET':
        roombaPositions = [{"x": x, "y":1, "z":z} for (a, x, z) in robotModel.grid.coord_iter() if isinstance(a, BorderAgent)]

        return jsonify({'positions':roombaPositions})
#trashcans
@app.route('/getTrashcan', methods=['GET'])
def getTrashcan():
    global robotModel

    if request.method == 'GET':
        # roombaPositions = [{"x": x, "y":1, "z":z} for (a, x, z) in robotModel.grid.coord_iter() if isinstance(a, Trashcan)]
        # stacks = [len(a.boxesStack) for (a,x,z) in robotModel.grid.coord_iter() if isinstance(a, Trashcan)]
        # roombaPositions = [{"x": agent.pos[0], "y":1, "z":agent.pos[1]} for agent in robotModel.schedule.agents if isinstance(agent, Trashcan)]
        # stacks = [len(agent.boxesStack) for agent in robotModel.schedule.agents if isinstance(agent, Trashcan)]
        
        roombaPositions, stacks = [], []

        for agent in robotModel.schedule.agents:
            if (isinstance(agent, Trashcan)):
                roombaPositions.append({"x": agent.pos[0], "y":1, "z":agent.pos[1]})
                stacks.append(len(agent.boxesStack))
        
        return jsonify({'positions':roombaPositions, 'stacks':stacks})


@app.route('/getBox', methods=['GET'])
def getBox():
    global robotModel

    if request.method == 'GET':
        # roombaPositions = [{"x": x, "y":1, "z":z} for (a, x, z) in robotModel.grid.coord_iter() if isinstance(a, Box)]
        roombaPositions = [{"x": agent.pos[0], "y":1, "z":agent.pos[1]} for agent in robotModel.schedule.agents if isinstance(agent, Box)]

        return jsonify({'positions':roombaPositions})

@app.route('/update', methods=['GET'])
def updateModel():
    global currentStep, robotModel
    if request.method == 'GET':
        robotModel.step()
        currentStep += 1
        return jsonify({'message':f'Model updated to step {currentStep}.', 'currentStep':currentStep})


if __name__=='__main__':
    app.run(host="localhost", port=8585, debug=True)

