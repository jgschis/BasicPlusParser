/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import { workspace, ExtensionContext, commands, window } from 'vscode';

import {
	CancellationToken,
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

import * as process from 'process';
import { mkdir, writeFile } from 'node:fs/promises';


let client: LanguageClient;
const SOURCE_CODE_BASE_DIR = "LanguageServerBasicPlusSourceCode";

const LOCAL_APP_DATA_DIR = process.env.LOCALAPPDATA;
if (LOCAL_APP_DATA_DIR == "" || LOCAL_APP_DATA_DIR == null){
	throw new Error("The environment variable LOCALAPPDATA is not defined.");
}

export async function activate(context: ExtensionContext) {


	const serverExe = 'dotnet';
	const serverDir = ".server";
	const serverDll = 'BasicPlusLangServer.dll';
	
	const serverModule = context.asAbsolutePath(
		path.join(serverDir, serverDll)
	);

	const serverOptions: ServerOptions = {
        run: { command: serverExe, args: [serverModule],transport: TransportKind.stdio,},
        debug: { command: serverExe, args: [serverModule], transport: TransportKind.stdio,}
    };


	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'basicplus' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
		}
		
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'basicplus-language-server',
		'Basicplus Language Server',
		serverOptions,
		clientOptions
	);

	// Start the client. This will also launch the server
	client.start();

	const disposable = commands.registerCommand('openInsight.OpenStoredProcs', async () => {
		await client.onReady();

		const  storedProcList =  await client.sendRequest("openInsight/GetStoredProcList", CancellationToken.None) as any[];

		const storedProcName = await window.showQuickPick(storedProcList.map( (v) =>v.name )) as string;

		if (storedProcName == "") {
			return;
		}

		window.showInformationMessage(storedProcName);
		
		const  sourceCode =  await client.sendRequest("openInsight/GetStoredProc", {StoredProcName:storedProcName}, CancellationToken.None) as string;
		
		if (sourceCode == "") {
			return;
		}
		
		const parts = storedProcName.split("*")

		if (parts.length == 0){
			return;
		}
		const fileName = parts[0]+".bp";

		let appName : string;
		if (parts.length > 1) {
			appName = parts[1];
		} else  {
			appName = "SYSPROG";
		}

		const sourceCodeDir = path.join(LOCAL_APP_DATA_DIR,SOURCE_CODE_BASE_DIR,appName);
		const createDir = await mkdir(sourceCodeDir, { recursive: true });
		await writeFile(path.join(sourceCodeDir,fileName),sourceCode);
		
	});
	
		
	context.subscriptions.push(disposable); 
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
