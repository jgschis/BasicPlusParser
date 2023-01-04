/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { Console } from 'console';
import * as path from 'path';
import { workspace, ExtensionContext, commands, window } from 'vscode';

import {
	CancellationToken,
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	const serverExe = 'dotnet';

	const serverModule = context.asAbsolutePath(
		path.join('.server', 'BasicPlusLangServer.dll')
	);

	const serverOptions: ServerOptions = {
        run: { command: serverExe, args: [serverModule],transport: TransportKind.stdio,},
        debug: { command: serverExe, args: [serverModule], transport: TransportKind.stdio,}
    };


	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		// Register the server for plain text documents
		documentSelector: [{ scheme: 'file', language: 'plaintext' }],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
		}
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'languageServerExample',
		'Language Server Example',
		serverOptions,
		clientOptions
	);

	
	// Start the client. This will also launch the server
	client.start();

	const disposable = commands.registerCommand('OpenInsight.OpenStoredProcs', () => {
		client.onReady().then(async () => {
			const  result =  await client.sendRequest("openInsight/GetStoredProcList", CancellationToken.None) as any[];
			const pick = await window.showQuickPick(result.map( (v) =>v.name )) as string;
			window.showInformationMessage(pick);
		});
	});
		
	context.subscriptions.push(disposable); 
		

}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
