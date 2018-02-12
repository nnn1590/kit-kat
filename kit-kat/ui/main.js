var app = angular.module('app', ['ngMaterial', 'ngAnimate']);

mainctrl.$inject = ['$scope', '$mdDialog', '$http'];

app.controller('mainctrl', mainctrl);

function mainctrl($scope, $mdDialog, $http) {

    // FormDrag
    $scope.formDrag = function() {
        callbackObj.formDrag();
    };
    // Changelog
    $scope.showChangelog = function() {
        $http.get("https://raw.githubusercontent.com/PR4GM4/kit-kat/master/CHANGELOG.txt").then(function(response) {
            $mdDialog.show(
              $mdDialog.alert()
                .parent(angular.element(document.querySelector('body')))
                .clickOutsideToClose(true)
                .title('Changelog (Updated to v'+window.clCurrv+')')
                .textContent(response.data)
                .ariaLabel('Changelog')
                .ok('Nice!')
            );
        });
    };

    $scope.connectToNTR = function(host) {
        callbackObj.connectToNTR(host);
    }
    $scope.disconnectFromNTR = function() {
        callbackObj.disconnectFromNTR();
    }
    $scope.validateIp = function(ip) {
        if (/^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/.test(ip)) {  
            return true;
        }
        return false;
    }

    $scope.settings = {
        log: "Created by PRAGMA\ntwitter.com/PRAGMA\nEnjoy!\n\n",
        open: false,
        ipAddress: "",
        connected: false,
        ntr: {
            autoConnect: false,
            showConsole: false,
            tScale: 1.0,
            bScale: 1.0,
            priority: 1,
            priorityFactor: 5,
            viewMode: 1,
            quality: 70,
            QoS: 101
        }
    }
    $scope.storeSettings = function() {
        var ntr = $scope.settings.ntr;
        callbackObj.storeSettings(ntr.autoConnect, ntr.showConsole, ntr.tScale.toString(), ntr.bScale.toString(), ntr.priority.toString(), ntr.priorityFactor.toString(), ntr.viewMode.toString(), ntr.quality.toString(), ntr.QoS.toString());
    }

};