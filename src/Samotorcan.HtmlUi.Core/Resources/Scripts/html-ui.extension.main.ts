﻿/// <reference path="references.ts" />

module htmlUi {
    var _controllerDataContainer = new ControllerDataContainer();
    var _initialized = false;

    export function init(): void {
        domAndScriptsReady(() => {
            if (_initialized)
                return;

            _initialized = true;

            // register functions
            htmlUi.native.registerFunction('syncControllerChanges', syncControllerChanges);

            // module
            var htmlUiModule = angular.module('htmlUi', []);

            // run
            htmlUiModule.run(['$rootScope', ($rootScope: ng.IRootScopeService) => {
                $rootScope['htmlUiControllerChanges'] = _controllerDataContainer.controllerChanges;

                addHtmlUiControllerChangesWatch($rootScope);
            }]);

            // controller
            htmlUiModule.factory('htmlUi.controller', [() => {
                var createObservableController = (controllerName: string, $scope: ng.IScope): ng.IScope => {
                    var scopeId = $scope.$id;

                    // create controller
                    var observableController = htmlUi.native.createObservableController(controllerName);

                    var controllerData = _controllerDataContainer.addControllerData(observableController.id);
                    controllerData.name = controllerName;
                    controllerData.$scope = $scope;
                    controllerData.scopeId = $scope.$id;

                    // properties
                    _.forEach(observableController.properties,(property) => {
                        var propertyName = property.name;
                        $scope[propertyName] = property.value;

                        // watch observable collection
                        if (_.isArray(property.value))
                            addCollectionWatch(propertyName, $scope);

                        // watch property
                        addPropertyWatch(propertyName, $scope);
                    });

                    // methods
                    _.forEach(observableController.methods,(method) => {
                        $scope[method.name] = () => {
                            return htmlUi.native.callMethod($scope.$id, method.name, utility.argumentsToArray(arguments));
                        };
                    });

                    // destroy controller
                    $scope.$on('$destroy',() => {
                        htmlUi.native.destroyController($scope.$id);
                    });

                    // warm up native calls
                    htmlUi.native.callInternalMethodAsync($scope.$id, 'warmUp', ['warmUp']).then(() => { });

                    return $scope;
                };

                return {
                    createObservableController: createObservableController
                };
            }]);

            // inject htmlUi module
            if (angular['resumeBootstrap'] == null) {
                angular['resumeDeferredBootstrap'] = function () {
                    angular['resumeBootstrap']([htmlUiModule.name]);
                };
            } else {
                angular['resumeBootstrap']([htmlUiModule.name]);
            }
        });
    }

    function domAndScriptsReady(func: () => void): void {
        domReady(() => {
            ensureScripts(func);
        });
    }

    function domReady(func: () => void): void {
        if (document.readyState === 'complete')
            func();
        else
            document.addEventListener("DOMContentLoaded", func);
    }

    function ensureScripts(onload?: () => void): void {
        var onloadFunctions: { [scriptName: string]: Function } = {};

        var loadScriptInternal = (scriptName: string): void => {
            var onloadInternal = () => {
                delete onloadFunctions[scriptName];

                if (Object.keys(onloadFunctions).length == 0 && onload != null)
                    onload();
            };

            onloadFunctions[scriptName] = onloadInternal;
            loadScript(scriptName, onloadInternal);
        };

        if (window['angular'] == null)
            loadScriptInternal('/Scripts/angular.js');

        if (window['_'] == null)
            loadScriptInternal('/Scripts/lodash.js');

        if (Object.keys(onloadFunctions).length == 0 && onload != null)
            onload();
    }

    function loadScript(scriptName: string, onload?: (ev: Event) => any) {
        var scriptElement = document.createElement('script');
        document.body.appendChild(scriptElement);

        if (onload != null)
            scriptElement.onload = onload;

        scriptElement.src = scriptName;
    }

    function addHtmlUiControllerChangesWatch($rootScope: ng.IRootScopeService): void {
        $rootScope.$watch('htmlUiControllerChanges', () => {
            if (!_controllerDataContainer.hasControllerChanges)
                return;

            try {
                htmlUi.native.syncControllerChanges(_controllerDataContainer.controllerChanges);
            } finally {
                _controllerDataContainer.clearControllerChanges();
            }
        }, true);
    }

    function addPropertyWatch(propertyName: string, $scope: ng.IScope): void {
        var scopeId = $scope.$id;
        var controllerData = _controllerDataContainer.getControllerDataByScopeId(scopeId);

        $scope.$watch(propertyName,(newValue, oldValue) => {
            if (newValue !== oldValue && !controllerData.hasPropertyValue(propertyName, newValue)) {
                controllerData.change.setProperty(propertyName, newValue);

                if (_.isArray(oldValue))
                    removeCollectionWatch(propertyName, $scope);

                if (_.isArray(newValue))
                    addCollectionWatch(propertyName, $scope);

                controllerData.change.removeObservableCollection(propertyName);
            }

            controllerData.removePropertyValue(propertyName);
        });
    }

    function addCollectionWatch(propertyName: string, $scope: ng.IScope): void {
        var scopeId = $scope.$id;
        var controllerData = _controllerDataContainer.getControllerDataByScopeId(scopeId);

        controllerData.addWatch(propertyName, $scope.$watchCollection(propertyName,(newCollection: any[], oldCollection: any[]) => {
            if (newCollection !== oldCollection && !utility.isArrayShallowEqual(newCollection, oldCollection) &&
                !controllerData.hasObservableCollectionValue(propertyName, newCollection) &&
                !controllerData.change.hasProperty(propertyName)) {

                var compareValues = _.zip(oldCollection, newCollection);

                _.forEach(compareValues, (compareValue, index) => {
                    var oldValue = compareValue[0];
                    var newValue = compareValue[1];

                    if (index < oldCollection.length && index < newCollection.length) {
                        // replace
                        if (oldValue !== newValue) {
                            controllerData.change.addObservableCollectionChange(propertyName,
                                ObservableCollectionChangeAction.Replace, newValue, index, null);
                        }
                    } else if (index < oldCollection.length && index >= newCollection.length) {
                        // remove
                        controllerData.change.addObservableCollectionChange(propertyName,
                            ObservableCollectionChangeAction.Remove, null, null, index);
                    } else {
                        // add
                        controllerData.change.addObservableCollectionChange(propertyName,
                            ObservableCollectionChangeAction.Add, newValue, index, null);
                    }
                });
            }

            controllerData.removeObservableCollectionValue(propertyName);
        }));
    }

    function removeCollectionWatch(propertyName: string, $scope: ng.IScope): void {
        var scopeId = $scope.$id;
        var controllerData = _controllerDataContainer.getControllerDataByScopeId(scopeId);

        controllerData.removeWatch(propertyName);
    }

    function syncControllerChanges(json: string): void {
        var controllerChanges = <ControllerChange[]>JSON.parse(json);

        _.forEach(controllerChanges, (controllerChange) => {
            var controllerId = controllerChange.id;
            var controllerData = _controllerDataContainer.getControllerData(controllerId);
            var controller = controllerData.$scope;

            controller.$apply(() => {
                // properties
                _.forEach(controllerChange.properties, (value, propertyName) => {
                    var propertyName = _.camelCase(propertyName);

                    controllerData.setControllerPropertyValue(propertyName, value);
                    controllerData.setPropertyValue(propertyName, value);
                });

                // observable collections
                _.forEach(controllerChange.observableCollections,(changes, propertyName) => {
                    var propertyName = _.camelCase(propertyName);

                    if (!_.isArray(controller[propertyName]))
                        controller[propertyName] = [];

                    var collection: any[] = controller[propertyName];

                    _.forEach(changes.actions,(change) => {
                        switch (change.action) {
                            case ObservableCollectionChangeAction.Add:
                                observableCollectionAddAction(collection, change);
                                break;
                            case ObservableCollectionChangeAction.Remove:
                                observableCollectionRemoveAction(collection, change);
                                break;
                            case ObservableCollectionChangeAction.Replace:
                                observableCollectionReplaceAction(collection, change);
                                break;
                            case ObservableCollectionChangeAction.Move:
                                observableCollectionMoveAction(collection, change);
                                break;
                        }
                    });

                    controllerData.setObservableCollectionValue(propertyName, utility.shallowCopyCollection(collection));
                });
            });
        });
    }

    function observableCollectionAddAction(collection: any[], change: ObservableCollectionChange): void {
        var insertIndex = change.newStartingIndex;
        var insertItems = change.newItems;

        _.forEach(insertItems, (insertItem) => {
            collection.splice(insertIndex, 0, insertItem);
            insertIndex++;
        });
    }

    function observableCollectionRemoveAction(collection: any[], change: ObservableCollectionChange): void {
        var removeIndex = change.oldStartingIndex;

        collection.splice(removeIndex, 1);
    }

    function observableCollectionReplaceAction(collection: any[], change: ObservableCollectionChange): void {
        var replaceIndex = change.newStartingIndex;
        var replaceItems = change.newItems;

        _.forEach(replaceItems, (replaceItem) => {
            collection[replaceIndex] = replaceItem;
            replaceIndex++;
        });
    }

    function observableCollectionMoveAction(collection: any[], change: ObservableCollectionChange): void {
        var fromIndex = change.oldStartingIndex;
        var toIndex = change.newStartingIndex;

        if (fromIndex == toIndex)
            return;

        var removedItems = collection.splice(fromIndex, 1);

        if (removedItems.length == 1) {
            var removedItem = removedItems[0];

            collection.splice(toIndex, 0, removedItem);
        }
    }
} 