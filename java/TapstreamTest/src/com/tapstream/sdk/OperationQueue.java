package com.tapstream.sdk;

import java.util.concurrent.ArrayBlockingQueue;

import junit.framework.TestCase;

class Operation {
	public String name;
	public String arg;

	public Operation(String name, String arg) {
		this.name = name;
		this.arg = arg;
	}
}

public class OperationQueue extends ArrayBlockingQueue<Operation> {
	private static final long serialVersionUID = 1L;

	public OperationQueue() {
		super(32);
	}

	public String expect(String opName) throws InterruptedException {
		Operation op = take();
		TestCase.assertEquals(opName, op.name);
		return op.arg;
	}

	public String expectEventually(String opName) throws InterruptedException {
		while(true) {
			Operation op = take();
			if(opName.equals(op.name)) {
				return op.arg;
			}
		}
	}
}
